// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagContainerInspectorPlugin : EditorInspectorPlugin
{
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
		if (name != nameof(ForgeTagContainer.ContainerTags))
		{
			return false;
		}

		var prop = new TagContainerEditorProperty();
		AddPropertyEditor(name, prop);
		return true;
	}
}
#endif
