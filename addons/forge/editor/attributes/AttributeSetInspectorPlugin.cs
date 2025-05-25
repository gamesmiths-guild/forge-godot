// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeSetInspectorPlugin : EditorInspectorPlugin
{
	private PackedScene? _inspectorScene;

	public override bool _CanHandle(GodotObject @object)
	{
		return @object is ForgeAttributeSet;
	}

	public override void _ParseCategory(GodotObject @object, string category)
	{
		if (category != "ForgeAttributeSet")
		{
			return;
		}

		_inspectorScene = ResourceLoader.Load<PackedScene>("uid://6h7g52vglco3");

		var containerScene = (AttributeSetEditor)_inspectorScene.Instantiate();
		containerScene.IsPluginInstance = true;
		containerScene.TargetAttributeSet = @object as ForgeAttributeSet;

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
#endif
