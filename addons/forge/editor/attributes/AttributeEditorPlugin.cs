// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeEditorPlugin : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject @object)
	{
		return @object is Resources.ForgeModifier
			|| @object is Resources.ForgeCue
			|| @object is Resources.ForgeEffectQuery
			|| @object is Resources.Components.ForgeAttributeRequirement;
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
		if (name is "Attribute" or "CapturedAttribute" or "MagnitudeAttribute" or "ModifyingAttribute")
		{
			// MagnitudeAttribute and ModifyingAttribute are filters rather than targets: leaving either unset is a
			// valid configuration, so the picker has to offer a way back to it. Attribute and CapturedAttribute are
			// required, and stay unclearable.
			AddPropertyEditor(name, new AttributeEditorProperty
			{
				AllowNone = name is "MagnitudeAttribute" or "ModifyingAttribute",
			});
			return true;
		}

		return false;
	}
}
#endif
