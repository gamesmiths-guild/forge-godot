// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.GameplayTags;

public partial class TagsInspectorPlugin : EditorInspectorPlugin
{
	private PackedScene? _inspectorScene;

	public override bool _CanHandle(GodotObject @object)
	{
		return @object is TagContainer;
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

		if (type == Variant.Type.Array && name == "ContainerTags")
		{
			var containerScene = (TagContainerEditor)_inspectorScene.Instantiate();
			containerScene.IsPluginInstance = true;

			if (@object is TagContainer tagContainer)
			{
				containerScene.ContainerTags = tagContainer.ContainerTags;
			}

			AddCustomControl(containerScene);

			return true;
		}

		return false;
	}
}
#endif
