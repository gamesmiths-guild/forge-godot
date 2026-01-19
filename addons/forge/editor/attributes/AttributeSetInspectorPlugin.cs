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

	public override bool _ParseProperty(
		GodotObject @object,
		Variant.Type type,
		string name,
		PropertyHint hintType,
		string hintString,
		PropertyUsageFlags usageFlags,
		bool wide)
	{
		if (name == "AttributeSetClass")
		{
			AddPropertyEditor(name, new AttributeSetClassEditorProperty());
			return true;
		}

		if (name == "InitialAttributeValues")
		{
			AddPropertyEditor(name, new AttributeSetValuesEditorProperty());
			return true;
		}

		return false;
	}
}
#endif
