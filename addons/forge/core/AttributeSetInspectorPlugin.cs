// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core.Godot;
using Godot;

namespace Gamesmiths.Forge.Editor;

[Tool]
public partial class AttributeSetInspectorPlugin : EditorInspectorPlugin
{
	public PackedScene InspectorScene { get; set; }

	public override bool _CanHandle(GodotObject @object)
	{
		return @object is AttributeSet;
	}

	public override void _ParseCategory(GodotObject @object, string category)
	{
		if (category != "AttributeSet")
		{
			return;
		}

		InspectorScene = ResourceLoader.Load<PackedScene>("res://addons/forge/core/AttributeSet.tscn");

		var containerScene = (AttributeSetEditor)InspectorScene.Instantiate();
		containerScene.IsPluginInstance = true;
		containerScene.TargetAttributeSet = @object as AttributeSet;

		AddCustomControl(containerScene);
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
		return true;
	}
}
