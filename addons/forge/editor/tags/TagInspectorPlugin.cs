// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

public partial class TagInspectorPlugin : EditorInspectorPlugin
{
	private PackedScene? _inspectorScene;

	public override bool _CanHandle(GodotObject @object)
	{
		return @object is ForgeTag;
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
		_inspectorScene = ResourceLoader.Load<PackedScene>("uid://cjgo744707fci");

		var tagEditorScene = (TagEditor)_inspectorScene.Instantiate();
		tagEditorScene.IsPluginInstance = true;

		if (@object is ForgeTag tag)
		{
			tagEditorScene.Tag = tag.Tag;

			tagEditorScene.TagChanged += (newTag) =>
			{
				EditorUndoRedoManager undo = EditorInterface.Singleton.GetEditorUndoRedo();
				var oldTag = tag.Tag;

				undo.CreateAction("Modify Tag");
				undo.AddDoProperty(tag, "Tag", newTag);
				undo.AddUndoProperty(tag, "Tag", oldTag);
				undo.CommitAction();
			};
		}

		AddPropertyEditor(name, tagEditorScene);

		return true;
	}
}
#endif
