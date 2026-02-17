// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Diagnostics;
using Gamesmiths.Forge.Godot.Editor;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Editor.Cues;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgePluginLoader : EditorPlugin
{
	private const string AutoloadPath = "uid://ba8fquhtwu5mu";

	private TagsEditorDock? _tagsEditorDock;
	private PanelContainer? _tagsEditorScene;
	private TagContainerInspectorPlugin? _tagContainerInspectorPlugin;
	private TagInspectorPlugin? _tagInspectorPlugin;
	private AttributeSetInspectorPlugin? _attributeSetInspectorPlugin;
	private CueHandlerInspectorPlugin? _cueHandlerInspectorPlugin;
	private AttributeEditorPlugin? _attributeEditorPlugin;
	private StatescriptGraphEditorDock? _statescriptGraphEditorDock;

	public override void _EnterTree()
	{
		_tagsEditorDock = new TagsEditorDock();
		AddDock(_tagsEditorDock);

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

		_statescriptGraphEditorDock = new StatescriptGraphEditorDock();
		_statescriptGraphEditorDock.SetUndoRedo(GetUndoRedo());
		AddDock(_statescriptGraphEditorDock);

		AddToolMenuItem("Repair assets tags", new Callable(this, MethodName.CallAssetRepairTool));
	}

	public override void _ExitTree()
	{
		Debug.Assert(
			_tagsEditorDock is not null,
			$"{nameof(_tagsEditorDock)} should have been initialized on _Ready().");

		RemoveDock(_tagsEditorDock);
		_tagsEditorDock.QueueFree();

		RemoveInspectorPlugin(_tagContainerInspectorPlugin);
		RemoveInspectorPlugin(_tagInspectorPlugin);
		RemoveInspectorPlugin(_attributeSetInspectorPlugin);
		RemoveInspectorPlugin(_cueHandlerInspectorPlugin);
		RemoveInspectorPlugin(_attributeEditorPlugin);

		RemoveDock(_statescriptGraphEditorDock);
		_statescriptGraphEditorDock.QueueFree();

		RemoveToolMenuItem("Repair assets tags");
	}

	public override bool _Handles(GodotObject @object)
	{
		return @object is StatescriptGraph;
	}

	public override void _Edit(GodotObject? @object)
	{
		if (@object is StatescriptGraph graph && _statescriptGraphEditorDock is not null)
		{
			_statescriptGraphEditorDock.OpenGraph(graph);
		}
	}

	public override void _MakeVisible(bool visible)
	{
		if (_statescriptGraphEditorDock is null)
		{
			return;
		}

		if (visible)
		{
			_statescriptGraphEditorDock.Open();
			_statescriptGraphEditorDock.Visible = visible;
		}
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
