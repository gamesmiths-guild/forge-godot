// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// Applies tag edits to a source, records them for undo/redo, and writes the result to disk.
/// </summary>
/// <remarks>
/// <para>
/// Both editing hosts - the Tags dock and the tag source inspector - go through this, so a tag edit behaves the same
/// whichever one made it. Saving immediately, rather than leaving the resource dirty, is deliberate: registering a tag
/// is project-global state that scenes and resources are validated against, so a tag that pickers already offer must
/// exist on disk. Otherwise closing without saving leaves scenes referencing a tag no source declares, which the asset
/// repair tool would then strip.
/// </para>
/// <para>
/// Undo targets the <see cref="ForgeTagsSource"/> rather than whichever editor made the change, because editors are
/// freed and rebuilt constantly while the resource outlives them.
/// </para>
/// <para>
/// It is a <see cref="Node"/> parented to the plugin rather than a bare <see cref="GodotObject"/> so that Godot owns
/// its lifetime. A bare object has to be disposed by hand, and the editor does not always run a plugin's
/// <c>_ExitTree</c> on shutdown, which shows up as a leaked instance.
/// </para>
/// </remarks>
[Tool]
public sealed partial class TagSourceEditingController : Node
{
	private EditorUndoRedoManager? _undoRedo;

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used to record edits.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager undoRedo)
	{
		_undoRedo = undoRedo;
	}

	/// <summary>
	/// Adds a tag to <paramref name="source"/>, normalizing the key first.
	/// </summary>
	/// <param name="source">The source to declare the tag in.</param>
	/// <param name="tagKey">The key as typed by the user.</param>
	/// <returns><see langword="true"/> when the tag was added.</returns>
	public bool AddTag(ForgeTagsSource source, string tagKey)
	{
		if (!ForgeTagsSource.TryNormalizeKey(tagKey, out string normalizedKey, out string error))
		{
			GD.PushWarning($"'{tagKey}' is not a usable tag key. {error}");
			return false;
		}

		string[]? newTags = source.WithTagAdded(normalizedKey);

		if (newTags is null)
		{
			GD.PushWarning($"Tag '{normalizedKey}' is already declared by this source.");
			return false;
		}

		Record($"Add Tag '{normalizedKey}'", source, [.. source.RegisteredTags], newTags);

		return true;
	}

	/// <summary>
	/// Removes a tag and every descendant of it from <paramref name="source"/>.
	/// </summary>
	/// <param name="source">The source to remove the tag from.</param>
	/// <param name="completeTagKey">The full dotted key of the tag.</param>
	public void RemoveTag(ForgeTagsSource source, string completeTagKey)
	{
		string[] oldTags = [.. source.RegisteredTags];
		string[] newTags = source.WithTagRemoved(completeTagKey);

		int removedCount = oldTags.Length - newTags.Length;

		if (removedCount == 0)
		{
			return;
		}

		string actionName = removedCount > 1
			? $"Remove Tag '{completeTagKey}' ({removedCount} tags)"
			: $"Remove Tag '{completeTagKey}'";

		Record(actionName, source, oldTags, newTags);
	}

	/// <summary>
	/// Writes <paramref name="source"/> to disk and refreshes every open tag editor.
	/// </summary>
	/// <param name="source">The source to save.</param>
	/// <remarks>
	/// Kept public, non-static and Godot-callable because undo/redo invokes it on this object as the second half of
	/// every recorded action.
	/// </remarks>
#pragma warning disable CA1822, S2325
	public void PersistSource(ForgeTagsSource source)
#pragma warning restore CA1822, S2325
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (string.IsNullOrEmpty(source.ResourcePath))
		{
			GD.PushError("This tag source has never been saved, so its tags cannot be written anywhere.");
			return;
		}

		Error error = ResourceSaver.Save(source);

		if (error != Error.Ok)
		{
			GD.PushError($"Failed to save tag source '{source.ResourcePath}': {error}");
			return;
		}

		ForgeTagsRegistry.Invalidate();
	}

	private void Record(string actionName, ForgeTagsSource source, string[] oldTags, string[] newTags)
	{
		EditorUndoRedoUtils.Record(
			_undoRedo,
			actionName,
			source,
			undo =>
			{
				undo.AddDoMethod(source, ForgeTagsSource.MethodName.ApplyRegisteredTags, newTags);
				undo.AddDoMethod(this, MethodName.PersistSource, source);
				undo.AddUndoMethod(source, ForgeTagsSource.MethodName.ApplyRegisteredTags, oldTags);
				undo.AddUndoMethod(this, MethodName.PersistSource, source);
			},
			execute: true,
			fallback: () =>
			{
				source.ApplyRegisteredTags(newTags);
				PersistSource(source);
			});
	}
}
#endif
