// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeEditorPlugin : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject @object)
	{
		return @object is Resources.ForgeModifier || @object is Resources.ForgeCue;
	}

	public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
	{
		if (name == "Attribute" || name == "CapturedAttribute" || name == "MagnitudeAttribute")
		{
			AddPropertyEditor(name, new AttributeEditorProperty());
			return true;
		}

		return false;
	}
}
#endif
