// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Diagnostics;
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
	private AttributeSetInspectorPlugin? _attributeSetInspectorPlugin;

	public override void _EnterTree()
	{
		PackedScene pluginScene = ResourceLoader.Load<PackedScene>(PluginScenePath);

		_dockedScene = (TabContainer)pluginScene.Instantiate();
		_dockedScene.GetNode<GameplayTagsEditor>("%Tags").IsPluginInstance = true;
		_dockedScene.GetNode<CueKeysEditor>("%Cues").IsPluginInstance = true;
		AddControlToDock(DockSlot.RightUl, _dockedScene);

		_tagsInspectorPlugin = new TagsInspectorPlugin();
		AddInspectorPlugin(_tagsInspectorPlugin);
		_attributeSetInspectorPlugin = new AttributeSetInspectorPlugin();
		AddInspectorPlugin(_attributeSetInspectorPlugin);

		AddToolMenuItem("Repair assets tags", new Callable(this, MethodName.CallAssetRepairTool));
	}

	public override void _ExitTree()
	{
		Debug.Assert(_dockedScene is not null, $"{nameof(_dockedScene)} should have been initialized on _Ready().");

		RemoveControlFromDocks(_dockedScene);
		_dockedScene.Free();

		RemoveInspectorPlugin(_tagsInspectorPlugin);
		RemoveInspectorPlugin(_attributeSetInspectorPlugin);

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
