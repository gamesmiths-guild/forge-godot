// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// Replaces a <see cref="ForgeTagsSource"/>'s raw tag array with the tree editor.
/// </summary>
[Tool]
public partial class TagsSourceInspectorPlugin : EditorInspectorPlugin
{
	private TagSourceEditingController? _controller;

	/// <summary>
	/// Sets the controller handed to every property editor this plugin creates. The controller already carries the
	/// editor's undo/redo manager, so nothing else has to be threaded through here.
	/// </summary>
	/// <param name="controller">The plugin's shared editing controller.</param>
	public void SetEditingController(TagSourceEditingController controller)
	{
		_controller = controller;
	}

	public override bool _CanHandle(GodotObject @object)
	{
		return @object is ForgeTagsSource;
	}

	public override bool _ParseProperty(
		GodotObject @object,
		Variant.Type type,
		string name,
		PropertyHint hintType,
		string hintString,
		PropertyUsageFlags usageFlags,
		bool wide)
	{
		if (name != nameof(ForgeTagsSource.RegisteredTags))
		{
			return false;
		}

		var property = new TagsSourceEditorProperty();

		if (_controller is not null)
		{
			property.SetEditingController(_controller);
		}

		AddPropertyEditor(name, property);

		return true;
	}
}
#endif
