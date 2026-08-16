// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Inspector plugin that replaces the default <see cref="ForgeSharedVariableSet.Variables"/> array editor with a
/// polished UI matching the graph variable panel style.
/// </summary>
public partial class SharedVariableSetInspectorPlugin : EditorInspectorPlugin
{
	private SharedVariableSetEditingController? _controller;

	/// <summary>
	/// Sets the controller that hosts undo/redo replays for the editors this plugin creates.
	/// </summary>
	/// <param name="controller">The shared editing controller from the editor plugin.</param>
	public void SetEditingController(SharedVariableSetEditingController controller)
	{
		_controller = controller;
	}

	/// <inheritdoc/>
	public override bool _CanHandle(GodotObject @object)
	{
		return @object is ForgeSharedVariableSet;
	}

	/// <inheritdoc/>
	public override bool _ParseProperty(
		GodotObject @object,
		Variant.Type type,
		string name,
		PropertyHint hintType,
		string hintString,
		PropertyUsageFlags usageFlags,
		bool wide)
	{
		if (name != "Variables")
		{
			return false;
		}

		var editorProperty = new SharedVariableSetEditorProperty();
		editorProperty.SetEditingController(_controller);
		AddPropertyEditor(name, editorProperty);
		return true;
	}
}
#endif
