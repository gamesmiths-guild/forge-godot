// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

public partial class TagInspectorPlugin : EditorInspectorPlugin
{
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
		if (name != "Tag")
		{
			return false;
		}

		var prop = new TagEditorProperty();
		AddPropertyEditor(name, prop);
		return true;
	}
}
#endif
