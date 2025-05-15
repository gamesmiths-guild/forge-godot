// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Diagnostics;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Editor;
using Godot;

using static Gamesmiths.Forge.Godot.Core.Forge;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgePluginLoader : EditorPlugin
{
	private const string AutoloadPath = "uid://ba8fquhtwu5mu";
	private const string PluginScenePath = "uid://pjscvogl6jak";

	private TabContainer? _dockedScene;
	private TagsInspectorPlugin? _tagsInspectorPlugin;
	private AttributeSetInspectorPlugin? _inspector;

	public override void _EnterTree()
	{
		ForgePluginData pluginData =
			ResourceLoader.Load<ForgePluginData>("uid://8j4xg16o3qnl");

		pluginData.RegisteredTags ??= [];

		TagsManager = new GameplayTagsManager([.. pluginData.RegisteredTags]);
		GD.Print("TagsManager Initialized");

		PackedScene pluginScene = ResourceLoader.Load<PackedScene>(PluginScenePath);

		_dockedScene = (TabContainer)pluginScene.Instantiate();
		_dockedScene.GetNode<GameplayTagsEditor>("%Tags").IsPluginInstance = true;
		_dockedScene.GetNode<CueKeysEditor>("%Cues").IsPluginInstance = true;
		AddControlToDock(DockSlot.RightUl, _dockedScene);

		_tagsInspectorPlugin = new TagsInspectorPlugin();
		AddInspectorPlugin(_tagsInspectorPlugin);
		_inspector = new AttributeSetInspectorPlugin();
		AddInspectorPlugin(_inspector);

		Script forgeEntityBaseScript = GD.Load<Script>("uid://8uj04dfe8oql");
		Script attributeSetBaseScript = GD.Load<Script>("uid://cxihb42t2mfqi");
		Script effectBaseScript = GD.Load<Script>("uid://dps0oef50noil");
		Script effectDataBaseScript = GD.Load<Script>("uid://b83hf13nj37k3");
		Texture2D forgeIcon = GD.Load<Texture2D>("uid://cu6ncpuumjo20");
		Texture2D attributeSetIcon = GD.Load<Texture2D>("uid://dnqaqpc02lx3p");
		Texture2D effectIcon = GD.Load<Texture2D>("uid://bpl454nqdpfjx");
		Texture2D effectDataIcon = GD.Load<Texture2D>("uid://obsk7rrtq1xd");
		AddCustomType("Forge Entity", "Node", forgeEntityBaseScript, forgeIcon);
		AddCustomType("Attribute Set", "Node", attributeSetBaseScript, attributeSetIcon);
		AddCustomType("Effect", "Node", effectBaseScript, effectIcon);
		AddCustomType("Effect Data", "Resource", effectDataBaseScript, effectDataIcon);

		AddToolMenuItem("Repair assets tags", new Callable(this, MethodName.CallAssetRepairTool));
	}

	public override void _ExitTree()
	{
		Debug.Assert(_dockedScene is not null, $"{nameof(_dockedScene)} should have been initialized on _Ready().");

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
