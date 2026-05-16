// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
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
	private QueryExpressionInspectorPlugin? _queryExpressionInspectorPlugin;
	private TagInspectorPlugin? _tagInspectorPlugin;
	private AttributeSetInspectorPlugin? _attributeSetInspectorPlugin;
	private CueHandlerInspectorPlugin? _cueHandlerInspectorPlugin;
	private AttributeEditorPlugin? _attributeEditorPlugin;
	private SharedVariableSetInspectorPlugin? _sharedVariableSetInspectorPlugin;
	private StatescriptGraphEditorDock? _statescriptGraphEditorDock;

	private EditorFileSystem? _fileSystem;
	private Callable _resourcesReimportedCallable;

	public override void _EnterTree()
	{
		EnsureForgeDataExists();

		_tagsEditorDock = new TagsEditorDock();
		AddDock(_tagsEditorDock);

		_tagContainerInspectorPlugin = new TagContainerInspectorPlugin();
		AddInspectorPlugin(_tagContainerInspectorPlugin);
		_queryExpressionInspectorPlugin = new QueryExpressionInspectorPlugin();
		AddInspectorPlugin(_queryExpressionInspectorPlugin);
		_tagInspectorPlugin = new TagInspectorPlugin();
		AddInspectorPlugin(_tagInspectorPlugin);
		_attributeSetInspectorPlugin = new AttributeSetInspectorPlugin();
		AddInspectorPlugin(_attributeSetInspectorPlugin);
		_cueHandlerInspectorPlugin = new CueHandlerInspectorPlugin();
		AddInspectorPlugin(_cueHandlerInspectorPlugin);
		_attributeEditorPlugin = new AttributeEditorPlugin();
		AddInspectorPlugin(_attributeEditorPlugin);
		_sharedVariableSetInspectorPlugin = new SharedVariableSetInspectorPlugin();
		_sharedVariableSetInspectorPlugin.SetUndoRedo(GetUndoRedo());
		AddInspectorPlugin(_sharedVariableSetInspectorPlugin);

		_statescriptGraphEditorDock = new StatescriptGraphEditorDock();
		_statescriptGraphEditorDock.SetUndoRedo(GetUndoRedo());
		AddDock(_statescriptGraphEditorDock);

		AddToolMenuItem("Repair assets tags", new Callable(this, MethodName.CallAssetRepairTool));

		_fileSystem = EditorInterface.Singleton.GetResourceFilesystem();
		_resourcesReimportedCallable = new Callable(this, nameof(OnResourcesReimported));

		_fileSystem.Connect(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable);

		Validation.Enabled = true;
	}

	public override void _ExitTree()
	{
		if (_fileSystem?.IsConnected(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable)
			== true)
		{
			_fileSystem.Disconnect(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable);
		}

		if (_tagsEditorDock is not null)
		{
			RemoveDock(_tagsEditorDock);
			_tagsEditorDock.Free();
			_tagsEditorDock = null;
		}

		RemoveInspectorPluginAndRelease(ref _tagContainerInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _queryExpressionInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _tagInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _attributeSetInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _cueHandlerInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _attributeEditorPlugin);
		RemoveInspectorPluginAndRelease(ref _sharedVariableSetInspectorPlugin);

		if (_statescriptGraphEditorDock is not null)
		{
			RemoveDock(_statescriptGraphEditorDock);
			_statescriptGraphEditorDock.Free();
			_statescriptGraphEditorDock = null;
		}

		_fileSystem = null;
		_resourcesReimportedCallable = default;

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

		EnsureForgeDataExists();

		bool config = ProjectSettings.LoadResourcePack(AutoloadPath);

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

		string[] paths = _statescriptGraphEditorDock.GetOpenResourcePaths();

		if (paths.Length == 0)
		{
			return;
		}

		configuration.SetValue("Forge", "open_tabs", string.Join(";", paths));
		configuration.SetValue("Forge", "active_tab", _statescriptGraphEditorDock.GetActiveTabIndex());

		bool[] varStates = _statescriptGraphEditorDock.GetVariablesPanelStates();
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

		string tabsString = tabsValue.AsString();
		if (string.IsNullOrEmpty(tabsString))
		{
			return;
		}

		string[] paths = tabsString.Split(';', StringSplitOptions.RemoveEmptyEntries);
		int activeIndex = active.AsInt32();

		bool[]? variablesStates = null;
		Variant varStatesValue = configuration.GetValue("Forge", "variables_states", string.Empty);
		string varString = varStatesValue.AsString();

		if (!string.IsNullOrEmpty(varString))
		{
			string[] parts = varString.Split(';');
			variablesStates = new bool[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				variablesStates[i] = bool.TryParse(parts[i], out bool v) && v;
			}
		}

		_statescriptGraphEditorDock.RestoreFromPaths(paths, activeIndex, variablesStates);
	}

	private static void EnsureForgeDataExists()
	{
		if (ResourceLoader.Exists(ForgeData.ForgeDataResourcePath))
		{
			return;
		}

		var forgeData = new ForgeData();
		Error error = ResourceSaver.Save(forgeData, ForgeData.ForgeDataResourcePath);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to create ForgeData resource: {error}");
			return;
		}

		EditorInterface.Singleton.GetResourceFilesystem().Scan();
		GD.Print("Created default ForgeData resource at ", ForgeData.ForgeDataResourcePath);
	}

	private static void CallAssetRepairTool()
	{
		AssetRepairTool.RepairAllAssetsTags();
	}

	private void RemoveInspectorPluginAndRelease<TPlugin>(ref TPlugin? plugin)
		where TPlugin : EditorInspectorPlugin
	{
		if (plugin is null)
		{
			return;
		}

		RemoveInspectorPlugin(plugin);
		plugin = null;
	}

	private void OnResourcesReimported(string[] resources)
	{
		foreach (string path in resources)
		{
			if (!ResourceLoader.Exists(path))
			{
				continue;
			}

			string fileType = EditorInterface.Singleton.GetResourceFilesystem().GetFileType(path);
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
