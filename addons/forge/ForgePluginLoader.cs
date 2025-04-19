// Copyright © 2025 Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Editor;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

using static Gamesmiths.Forge.Godot.Forge;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgePluginLoader : EditorPlugin
{
	private const string AutoloadPath = "res://addons/forge/Forge.cs";
	private const string PluginScenePath = "res://addons/forge/gameplay_tags/GameplayTags.tscn";

	private GameplayTagsUI _dockedScene;
	private TagsInspectorPlugin _tagsInspectorPlugin;
	private AttributeSetInspectorPlugin _inspector;

	public PackedScene PluginScene { get; set; }

	public override void _EnterTree()
	{
		RegisteredTags registeredTags =
			ResourceLoader.Load<RegisteredTags>("res://addons/forge/gameplay_tags/registered_tags.tres");
		TagsManager = new GameplayTagsManager([.. registeredTags.Tags]);
		GD.Print("TagsManager Initialized");

		PluginScene = ResourceLoader.Load<PackedScene>(PluginScenePath);

		_dockedScene = (GameplayTagsUI)PluginScene.Instantiate();
		_dockedScene.Name = "Gameplay Tags";
		_dockedScene.IsPluginInstance = true;
		AddControlToDock(DockSlot.RightUl, _dockedScene);

		_tagsInspectorPlugin = new TagsInspectorPlugin();
		AddInspectorPlugin(_tagsInspectorPlugin);
		_inspector = new AttributeSetInspectorPlugin();
		AddInspectorPlugin(_inspector);

		Script forgeEntityBaseScript = GD.Load<Script>("res://addons/forge/core/ForgeEntity.cs");
		Script attributeSetBaseScript = GD.Load<Script>("res://addons/forge/core/AttributeSet.cs");
		Script effectBaseScript = GD.Load<Script>("res://addons/forge/gameplay_effects/GameplayEffect.cs");
		Script effectDataBaseScript = GD.Load<Script>("res://addons/forge/gameplay_effects/GameplayEffectData.cs");
		Texture2D forgeIcon = GD.Load<Texture2D>("res://addons/forge/anvil.svg");
		Texture2D attributeSetIcon = GD.Load<Texture2D>("res://addons/forge/attributes.svg");
		Texture2D effectIcon = GD.Load<Texture2D>("res://addons/forge/effect.svg");
		Texture2D effectDataIcon = GD.Load<Texture2D>("res://addons/forge/effect_data.svg");
		AddCustomType("Forge Entity", "Node", forgeEntityBaseScript, forgeIcon);
		AddCustomType("Attribute Set", "Node", attributeSetBaseScript, attributeSetIcon);
		AddCustomType("Effect", "Node", effectBaseScript, effectIcon);
		AddCustomType("Effect Data", "Resource", effectDataBaseScript, effectDataIcon);

		AddToolMenuItem("Repair assets tags", new Callable(this, MethodName.CallAssetRepairTool));
	}

	public override void _ExitTree()
	{
		RemoveControlFromDocks(_dockedScene);
		_dockedScene.Free();

		RemoveInspectorPlugin(_tagsInspectorPlugin);
		RemoveInspectorPlugin(_inspector);

		RemoveCustomType("Forge Entity");
		RemoveCustomType("Attribute Set");
		RemoveCustomType("Effect");
		RemoveCustomType("Effect Data");

		RemoveToolMenuItem("Repair assets tags");

		TagsManager?.DestroyTagTree();
	}

	public override void _EnablePlugin()
	{
		base._EnablePlugin();

		var config = ProjectSettings.LoadResourcePack(AutoloadPath);

		if (config)
		{
			GD.PrintErr("Failed to load script at res://addons/forge/Forge.cs");
			return;
		}

		if (!ProjectSettings.HasSetting("autoload/Forge"))
		{
			ProjectSettings.SetSetting("autoload/Forge", AutoloadPath);
			ProjectSettings.Save();
		}
	}

	public override void _DisablePlugin()
	{
		if (ProjectSettings.HasSetting("autoload/Forge"))
		{
			ProjectSettings.Clear("autoload/Forge");
			ProjectSettings.Save();
		}
	}

	private static void CallAssetRepairTool()
	{
		AssetRepairTool.RepairAllAssetsTags();
	}
}
#endif
