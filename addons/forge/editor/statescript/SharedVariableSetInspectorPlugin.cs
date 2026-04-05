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
	private EditorUndoRedoManager? _undoRedo;

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager undoRedo)
	{
		_undoRedo = undoRedo;
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
		editorProperty.SetUndoRedo(_undoRedo);
		AddPropertyEditor(name, editorProperty);
		return true;
	}
}
#endif
