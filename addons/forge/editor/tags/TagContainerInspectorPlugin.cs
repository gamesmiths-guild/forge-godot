// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

public partial class TagContainerInspectorPlugin : EditorInspectorPlugin
{
	private PackedScene? _inspectorScene;

	public override bool _CanHandle(GodotObject @object)
	{
		return @object is ForgeTagContainer;
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
		_inspectorScene = ResourceLoader.Load<PackedScene>("uid://tou2hv4cet4e");

		var containerScene = (TagContainerEditor)_inspectorScene.Instantiate();
		containerScene.IsPluginInstance = true;

		if (@object is ForgeTagContainer tagContainer)
		{
			tagContainer.ContainerTags ??= [];
			containerScene.ContainerTags = tagContainer.ContainerTags;

			containerScene.TagsChanged += () =>
			{
				Array<string> oldValue = tagContainer.ContainerTags ?? [];
				Array<string> newValue = containerScene.ContainerTags ?? [];

				var oldCopy = new Array<string>();
				oldCopy.AddRange(oldValue);

				var newCopy = new Array<string>();
				newCopy.AddRange(newValue);

				EditorUndoRedoManager undo = EditorInterface.Singleton.GetEditorUndoRedo();
				undo.CreateAction("Modify Tag Container");
				undo.AddDoProperty(tagContainer, "ContainerTags", newCopy);
				undo.AddUndoProperty(tagContainer, "ContainerTags", oldCopy);
				undo.CommitAction();
			};
		}

		AddPropertyEditor(name, containerScene);

		return true;
	}
}
#endif
