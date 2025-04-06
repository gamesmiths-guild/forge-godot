// Copyright © 2025 Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.GameplayTags.Godot;

public partial class TagsInspectorPlugin : EditorInspectorPlugin
{
	public PackedScene InspectorScene { get; set; }

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
		InspectorScene = ResourceLoader.Load<PackedScene>("res://addons/forge/gameplay_tags/TagContainer.tscn");

		if (type == Variant.Type.Array && name == "ContainerTags")
		{
			var containerScene = (TagContainerEditor)InspectorScene.Instantiate();
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
