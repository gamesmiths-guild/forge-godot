// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Diagnostics;
using Gamesmiths.Forge.Godot.Editor;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Editor.Cues;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgePluginLoader : EditorPlugin
{
	private const string AutoloadPath = "uid://ba8fquhtwu5mu";
	private const string PluginScenePath = "uid://pjscvogl6jak";

	private EditorDock? _editorDock;
	private PanelContainer? _dockedScene;
	private TagContainerInspectorPlugin? _tagContainerInspectorPlugin;
	private TagInspectorPlugin? _tagInspectorPlugin;
	private AttributeSetInspectorPlugin? _attributeSetInspectorPlugin;
	private CueHandlerInspectorPlugin? _cueHandlerInspectorPlugin;
	private AttributeEditorPlugin? _attributeEditorPlugin;

	public override void _EnterTree()
	{
		PackedScene pluginScene = ResourceLoader.Load<PackedScene>(PluginScenePath);

		_editorDock = new EditorDock
		{
			Title = "Forge",
			DockIcon = GD.Load<Texture2D>("uid://cu6ncpuumjo20"),
			DefaultSlot = EditorDock.DockSlot.RightUl,
		};

		_dockedScene = (PanelContainer)pluginScene.Instantiate();
		_dockedScene.GetNode<TagsEditor>("%Tags").IsPluginInstance = true;

		_editorDock.AddChild(_dockedScene);
		AddDock(_editorDock);

		_tagContainerInspectorPlugin = new TagContainerInspectorPlugin();
		AddInspectorPlugin(_tagContainerInspectorPlugin);
		_tagInspectorPlugin = new TagInspectorPlugin();
		AddInspectorPlugin(_tagInspectorPlugin);
		_attributeSetInspectorPlugin = new AttributeSetInspectorPlugin();
		AddInspectorPlugin(_attributeSetInspectorPlugin);
		_cueHandlerInspectorPlugin = new CueHandlerInspectorPlugin();
		AddInspectorPlugin(_cueHandlerInspectorPlugin);
		_attributeEditorPlugin = new AttributeEditorPlugin();
		AddInspectorPlugin(_attributeEditorPlugin);

		AddToolMenuItem("Repair assets tags", new Callable(this, MethodName.CallAssetRepairTool));
	}

	public override void _ExitTree()
	{
		Debug.Assert(_editorDock is not null, $"{nameof(_editorDock)} should have been initialized on _Ready().");
		Debug.Assert(_dockedScene is not null, $"{nameof(_dockedScene)} should have been initialized on _Ready().");

		RemoveDock(_editorDock);
		_editorDock.QueueFree();
		_dockedScene.Free();

		RemoveInspectorPlugin(_tagContainerInspectorPlugin);
		RemoveInspectorPlugin(_tagInspectorPlugin);
		RemoveInspectorPlugin(_attributeSetInspectorPlugin);
		RemoveInspectorPlugin(_cueHandlerInspectorPlugin);
		RemoveInspectorPlugin(_attributeEditorPlugin);

		RemoveToolMenuItem("Repair assets tags");
	}

	public override void _EnablePlugin()
	{
		base._EnablePlugin();

		var config = ProjectSettings.LoadResourcePack(AutoloadPath);

		if (config)
		{
			GD.PrintErr("Failed to load script at res://addons/forge/core/ForgeBootstrap.cs");
			return;
		}

		if (!ProjectSettings.HasSetting("autoload/Forge Bootstrap"))
		{
			ProjectSettings.SetSetting("autoload/Forge Bootstrap", AutoloadPath);
			ProjectSettings.Save();
		}
	}

	public override void _DisablePlugin()
	{
		if (ProjectSettings.HasSetting("autoload/Forge Bootstrap"))
		{
			ProjectSettings.Clear("autoload/Forge Bootstrap");
			ProjectSettings.Save();
		}
	}

	private static void CallAssetRepairTool()
	{
		AssetRepairTool.RepairAllAssetsTags();
	}
}
#endif
