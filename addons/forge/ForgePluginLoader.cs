// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
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
	private TagContainerInspectorPlugin? _tagContainerInspectorPlugin;
	private TagInspectorPlugin? _tagInspectorPlugin;
	private AttributeSetInspectorPlugin? _attributeSetInspectorPlugin;
	private CueHandlerInspectorPlugin? _cueHandlerInspectorPlugin;
	private AttributeEditorPlugin? _attributeEditorPlugin;
	private StatescriptGraphEditorDock? _statescriptGraphEditorDock;

	private EditorFileSystem? _fileSystem;
	private Callable _resourcesReimportedCallable;

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

		_fileSystem = EditorInterface.Singleton.GetResourceFilesystem();
		_resourcesReimportedCallable = new Callable(this, nameof(OnResourcesReimported));

		_fileSystem.Connect(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable);
	}

	public override void _ExitTree()
	{
		Debug.Assert(
			_tagsEditorDock is not null,
			$"{nameof(_tagsEditorDock)} should have been initialized on _Ready().");
		Debug.Assert(
			_statescriptGraphEditorDock is not null,
			$"{nameof(_statescriptGraphEditorDock)} should have been initialized on _Ready().");

		if (_fileSystem?.IsConnected(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable)
			== true)
		{
			_fileSystem.Disconnect(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable);
		}

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
		}

		_statescriptGraphEditorDock.Visible = visible;
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

	public override void _SaveExternalData()
	{
		_statescriptGraphEditorDock?.SaveAllOpenGraphs();
	}

	public override string _GetPluginName()
	{
		return "Forge";
	}

	public override void _GetWindowLayout(ConfigFile configuration)
	{
		if (_statescriptGraphEditorDock is null)
		{
			return;
		}

		var paths = _statescriptGraphEditorDock.GetOpenResourcePaths();

		if (paths.Length == 0)
		{
			return;
		}

		configuration.SetValue("Forge", "open_tabs", string.Join(";", paths));
		configuration.SetValue("Forge", "active_tab", _statescriptGraphEditorDock.GetActiveTabIndex());

		var varStates = _statescriptGraphEditorDock.GetVariablesPanelStates();
		configuration.SetValue("Forge", "variables_states", string.Join(";", varStates));
	}

	public override void _SetWindowLayout(ConfigFile configuration)
	{
		if (_statescriptGraphEditorDock is null)
		{
			return;
		}

		Variant tabsValue = configuration.GetValue("Forge", "open_tabs", string.Empty);
		Variant active = configuration.GetValue("Forge", "active_tab", -1);

		var tabsString = tabsValue.AsString();
		if (string.IsNullOrEmpty(tabsString))
		{
			return;
		}

		var paths = tabsString.Split(';', StringSplitOptions.RemoveEmptyEntries);
		var activeIndex = active.AsInt32();

		bool[]? variablesStates = null;
		Variant varStatesValue = configuration.GetValue("Forge", "variables_states", string.Empty);
		var varString = varStatesValue.AsString();

		if (!string.IsNullOrEmpty(varString))
		{
			var parts = varString.Split(';');
			variablesStates = new bool[parts.Length];
			for (var i = 0; i < parts.Length; i++)
			{
				variablesStates[i] = bool.TryParse(parts[i], out var v) && v;
			}
		}

		_statescriptGraphEditorDock.RestoreFromPaths(paths, activeIndex, variablesStates);
	}

	private static void CallAssetRepairTool()
	{
		AssetRepairTool.RepairAllAssetsTags();
	}

	private void OnResourcesReimported(string[] resources)
	{
		foreach (var path in resources)
		{
			if (!ResourceLoader.Exists(path))
			{
				continue;
			}

			var fileType = EditorInterface.Singleton.GetResourceFilesystem().GetFileType(path);
			if (fileType != "StatescriptGraph" && fileType != "Resource")
			{
				continue;
			}

			Resource resource = ResourceLoader.Load(path);
			if (resource is StatescriptGraph graph)
			{
				_statescriptGraphEditorDock?.OpenGraph(graph);
			}
		}
	}
}
#endif
