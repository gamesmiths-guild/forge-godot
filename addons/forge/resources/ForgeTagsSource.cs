// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// A file holding a set of registered gameplay tags.
/// </summary>
/// <remarks>
/// <para>
/// A project reads its tags from every source listed in <see cref="ForgeSettings.SourcesSetting"/>, and the runtime
/// registry is their union: duplicates are ignored, and a tag exists for as long as any source declares it. Sources are
/// peers - none of them is privileged - so splitting tags across files (cooldowns in one, effects in another) is purely
/// an organizational choice, and a source can be copied between projects on its own.
/// </para>
/// <para>
/// The hierarchy is implicit: declaring <c>a.b.c</c> also creates <c>a</c> and <c>a.b</c>. A parent therefore lives for
/// as long as some source still declares a descendant of it.
/// </para>
/// </remarks>
[Tool]
[GlobalClass]
[Icon("uid://c1qxn8vhaw3md")]
public partial class ForgeTagsSource : Resource
{
	/// <summary>
	/// Gets or sets the tag keys declared by this source.
	/// </summary>
	/// <remarks>
	/// Prefer <see cref="WithTagAdded(string)"/> and <see cref="WithTagRemoved(string)"/> over editing this in place:
	/// they apply the project's case-insensitive matching rules and produce the before/after pair undo/redo needs.
	/// </remarks>
	[Export]
	public Array<string> RegisteredTags { get; set; } = [];

	/// <summary>
	/// Loads every tag source configured for this project, in the configured order.
	/// </summary>
	/// <returns>The sources that resolved successfully.</returns>
	public static List<ForgeTagsSource> LoadSources()
	{
		var sources = new List<ForgeTagsSource>();

		foreach (string reference in ForgeSettings.GetSourceReferences())
		{
			ForgeTagsSource? source = LoadReference(reference);

			if (source is null)
			{
				GD.PushWarning(
					$"Forge tag source '{reference}' could not be loaded and was skipped. Remove it from " +
					$"'{ForgeSettings.SourcesSetting}' in Project Settings if it no longer exists.");
				continue;
			}

			sources.Add(source);
		}

		return sources;
	}

	/// <summary>
	/// Gets every tag registered by this project, as the union of all configured sources.
	/// </summary>
	/// <returns>The registered tag keys, with duplicates removed.</returns>
	public static string[] LoadRegisteredTags()
	{
		return
		[
			.. LoadSources()
				.SelectMany(source => source.RegisteredTags)
				.Distinct(StringComparer.OrdinalIgnoreCase),
		];
	}

	/// <summary>
	/// Builds a <see cref="TagsManager"/> over the union of all configured sources.
	/// </summary>
	/// <returns>A manager holding every tag registered by this project.</returns>
	public static TagsManager CreateTagsManager()
	{
		return new TagsManager(LoadRegisteredTags());
	}

	/// <summary>
	/// Normalizes a user-typed tag key, repairing it when possible.
	/// </summary>
	/// <param name="input">The key as typed.</param>
	/// <param name="normalized">The key to use, which may differ from <paramref name="input"/>.</param>
	/// <param name="error">Describes the correction that was applied, or why the input was rejected.</param>
	/// <returns><see langword="true"/> when <paramref name="normalized"/> is usable.</returns>
	public static bool TryNormalizeKey(string input, out string normalized, out string error)
	{
		if (Tag.IsValidKey(input, out error, out string fixedKey))
		{
			normalized = input;
			return true;
		}

		// IsValidKey repairs what it can and leaves fixedKey empty when nothing usable remains.
		normalized = fixedKey ?? string.Empty;

		return !string.IsNullOrWhiteSpace(normalized);
	}

	/// <summary>
	/// Determines whether this source declares <paramref name="completeTagKey"/> itself.
	/// </summary>
	/// <param name="completeTagKey">The full dotted key of the tag.</param>
	/// <returns><see langword="true"/> when the key is declared here.</returns>
	public bool DeclaresTag(string completeTagKey)
	{
		return RegisteredTags.Any(tag => string.Equals(tag, completeTagKey, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Determines whether this source declares <paramref name="completeTagKey"/> or any descendant of it, which is what
	/// makes the tag show up in this source's branch of the tree.
	/// </summary>
	/// <param name="completeTagKey">The full dotted key of the tag.</param>
	/// <returns><see langword="true"/> when the key or a descendant of it is declared here.</returns>
	public bool DeclaresTagOrDescendant(string completeTagKey)
	{
		string descendantPrefix = completeTagKey + ".";

		return RegisteredTags.Any(tag =>
			string.Equals(tag, completeTagKey, StringComparison.OrdinalIgnoreCase)
			|| tag.StartsWith(descendantPrefix, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Produces the tag list this source would have with <paramref name="completeTagKey"/> added.
	/// </summary>
	/// <param name="completeTagKey">The full dotted key of the tag to add.</param>
	/// <returns>
	/// The resulting tag list, or <see langword="null"/> when this source already declares the key - including as a
	/// different casing, since tags are matched case-insensitively at runtime.
	/// </returns>
	public string[]? WithTagAdded(string completeTagKey)
	{
		if (DeclaresTag(completeTagKey))
		{
			return null;
		}

		return [.. RegisteredTags, completeTagKey];
	}

	/// <summary>
	/// Produces the tag list this source would have with <paramref name="completeTagKey"/> and every descendant of it
	/// removed.
	/// </summary>
	/// <param name="completeTagKey">The full dotted key of the tag to remove.</param>
	/// <returns>The resulting tag list.</returns>
	/// <remarks>
	/// Removing the last child of an implicit parent removes that parent from the tree as well, which is correct under
	/// an implicit hierarchy: the parent only ever existed because a descendant declared it. Other sources declaring
	/// the same branch are unaffected.
	/// </remarks>
	public string[] WithTagRemoved(string completeTagKey)
	{
		string descendantPrefix = completeTagKey + ".";

		return
		[
			.. RegisteredTags.Where(tag =>
				!string.Equals(tag, completeTagKey, StringComparison.OrdinalIgnoreCase)
				&& !tag.StartsWith(descendantPrefix, StringComparison.OrdinalIgnoreCase)),
		];
	}

	/// <summary>
	/// Replaces this source's tags and notifies anything observing the resource.
	/// </summary>
	/// <param name="tags">The tags this source should declare.</param>
	/// <remarks>
	/// Kept Godot-callable so undo/redo can target the resource itself rather than whichever editor happened to make
	/// the change - editors are freed and rebuilt on every assembly reload, the resource is not.
	/// </remarks>
	public void ApplyRegisteredTags(string[] tags)
	{
		RegisteredTags.Clear();
		RegisteredTags.AddRange(tags);

		EmitChanged();
	}

	private static ForgeTagsSource? LoadReference(string reference)
	{
		string path = ForgeSettings.ResolveReference(reference);

		if (!ResourceLoader.Exists(path))
		{
			return null;
		}

		return ResourceLoader.Load(path) as ForgeTagsSource;
	}
}
