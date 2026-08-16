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
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Attributes;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgePluginLoader : EditorPlugin
{
	private const string AutoloadPath = "uid://ba8fquhtwu5mu";
	private const string RepairToolItemText = "Repair assets tags";
	private const int RepairToolItemId = 0;

	private TagSourceEditingController? _tagEditingController;
	private TagsEditorDock? _tagsEditorDock;
	private TagsSourceInspectorPlugin? _tagsSourceInspectorPlugin;
	private TagContainerInspectorPlugin? _tagContainerInspectorPlugin;
	private QueryExpressionInspectorPlugin? _queryExpressionInspectorPlugin;
	private TagInspectorPlugin? _tagInspectorPlugin;
	private AttributeSetInspectorPlugin? _attributeSetInspectorPlugin;
	private CueHandlerInspectorPlugin? _cueHandlerInspectorPlugin;
	private AttributeEditorPlugin? _attributeEditorPlugin;
	private AttributeSetDefinitionInspectorPlugin? _attributeSetDefinitionInspectorPlugin;
	private SharedVariableSetInspectorPlugin? _sharedVariableSetInspectorPlugin;
	private SharedVariableSetEditingController? _sharedVariableSetEditingController;
	private StatescriptGraphEditorDock? _statescriptGraphEditorDock;

	private AssetRepairDialog? _repairDialog;
	private PopupMenu? _toolsMenu;

	private EditorFileSystem? _fileSystem;
	private Callable _resourcesReimportedCallable;
	private Callable _resourcesReloadCallable;
	private Callable _toolsMenuIdPressedCallable;

	public override void _EnterTree()
	{
		EditorUndoRedoUtils.ResetScopes();

		ForgeSettings.EnsureRegistered();
		EnsureTagSourceExists();

		// One controller for the whole plugin lifetime: undo/redo entries call back into it, so it has to outlive the
		// dock and every inspector that records an edit through it.
		_tagEditingController = new TagSourceEditingController();
		_tagEditingController.SetUndoRedo(GetUndoRedo());
		AddChild(_tagEditingController);

		_tagsEditorDock = new TagsEditorDock();
		_tagsEditorDock.SetEditingController(_tagEditingController);
		AddDock(_tagsEditorDock);

		_tagsSourceInspectorPlugin = new TagsSourceInspectorPlugin();
		_tagsSourceInspectorPlugin.SetEditingController(_tagEditingController);
		AddInspectorPlugin(_tagsSourceInspectorPlugin);

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
		_attributeSetDefinitionInspectorPlugin = new AttributeSetDefinitionInspectorPlugin();
		AddInspectorPlugin(_attributeSetDefinitionInspectorPlugin);

		// Same reasoning as the tag controller: it has to outlive the property editors the inspector rebuilds.
		_sharedVariableSetEditingController = new SharedVariableSetEditingController();
		_sharedVariableSetEditingController.SetUndoRedo(GetUndoRedo());
		AddChild(_sharedVariableSetEditingController);

		_sharedVariableSetInspectorPlugin = new SharedVariableSetInspectorPlugin();
		_sharedVariableSetInspectorPlugin.SetEditingController(_sharedVariableSetEditingController);
		AddInspectorPlugin(_sharedVariableSetInspectorPlugin);

		_statescriptGraphEditorDock = new StatescriptGraphEditorDock();
		_statescriptGraphEditorDock.SetUndoRedo(GetUndoRedo());
		AddDock(_statescriptGraphEditorDock);

		_repairDialog = new AssetRepairDialog();
		AddChild(_repairDialog);

		_tagsEditorDock.SetRepairDialog(_repairDialog);

		_toolsMenu = new PopupMenu();
		_toolsMenu.AddItem(RepairToolItemText, RepairToolItemId);

		_toolsMenuIdPressedCallable = new Callable(this, MethodName.OnToolsMenuIdPressed);
		_toolsMenu.Connect(PopupMenu.SignalName.IdPressed, _toolsMenuIdPressedCallable);

		AddToolSubmenuItem("Forge", _toolsMenu);

		_fileSystem = EditorInterface.Singleton.GetResourceFilesystem();
		_resourcesReimportedCallable = new Callable(this, nameof(OnResourcesReimported));
		_resourcesReloadCallable = new Callable(this, nameof(OnResourcesReload));

		_fileSystem.Connect(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable);

		_fileSystem.Connect(EditorFileSystem.SignalName.ResourcesReload, _resourcesReloadCallable);

		ProjectSettings.SettingsChanged += OnProjectSettingsChanged;

		// Saving an attribute set definition rewrites its generated class, so the code follows the resource without the
		// user having to remember to regenerate.
		ResourceSaved += OnResourceSaved;

		Validation.Enabled = true;
	}

	public override void _ExitTree()
	{
		ProjectSettings.SettingsChanged -= OnProjectSettingsChanged;
		ResourceSaved -= OnResourceSaved;

		if (_fileSystem?.IsConnected(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable)
			== true)
		{
			_fileSystem.Disconnect(EditorFileSystem.SignalName.ResourcesReimported, _resourcesReimportedCallable);
		}

		if (_fileSystem?.IsConnected(EditorFileSystem.SignalName.ResourcesReload, _resourcesReloadCallable) == true)
		{
			_fileSystem.Disconnect(EditorFileSystem.SignalName.ResourcesReload, _resourcesReloadCallable);
		}

		ForgeTagsRegistry.Release();

		if (_tagsEditorDock is not null)
		{
			RemoveDock(_tagsEditorDock);
			_tagsEditorDock.Free();
			_tagsEditorDock = null;
		}

		RemoveInspectorPluginAndRelease(ref _tagsSourceInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _tagContainerInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _queryExpressionInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _tagInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _attributeSetInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _cueHandlerInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _attributeEditorPlugin);
		RemoveInspectorPluginAndRelease(ref _attributeSetDefinitionInspectorPlugin);
		RemoveInspectorPluginAndRelease(ref _sharedVariableSetInspectorPlugin);

		if (_statescriptGraphEditorDock is not null)
		{
			_statescriptGraphEditorDock.Release();
			RemoveDock(_statescriptGraphEditorDock);
			_statescriptGraphEditorDock.Free();
			_statescriptGraphEditorDock = null;
		}

		if (_tagEditingController is not null)
		{
			RemoveChild(_tagEditingController);
			_tagEditingController.QueueFree();
			_tagEditingController = null;
		}

		if (_sharedVariableSetEditingController is not null)
		{
			RemoveChild(_sharedVariableSetEditingController);
			_sharedVariableSetEditingController.QueueFree();
			_sharedVariableSetEditingController = null;
		}

		_fileSystem = null;
		_resourcesReimportedCallable = default;
		_resourcesReloadCallable = default;

		if (_toolsMenu is not null && IsInstanceValid(_toolsMenu)
			&& _toolsMenu.IsConnected(PopupMenu.SignalName.IdPressed, _toolsMenuIdPressedCallable))
		{
			_toolsMenu.Disconnect(PopupMenu.SignalName.IdPressed, _toolsMenuIdPressedCallable);
		}

		_toolsMenuIdPressedCallable = default;

		RemoveToolMenuItem("Forge");

		if (_toolsMenu is not null && IsInstanceValid(_toolsMenu))
		{
			_toolsMenu.QueueFree();
		}

		_toolsMenu = null;

		if (_repairDialog is not null)
		{
			RemoveChild(_repairDialog);
			_repairDialog.QueueFree();
			_repairDialog = null;
		}
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

		ForgeSettings.EnsureRegistered();
		EnsureTagSourceExists();

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

	/// <summary>
	/// Makes sure the project has somewhere to put its tags, by creating a first source when none is configured.
	/// </summary>
	/// <remarks>
	/// A configured source that fails to resolve is left alone rather than pruned or replaced: it usually means the
	/// file was moved or the UID cache has not rebuilt yet, and the Tags dock reports it as missing so the user can
	/// decide. Silently manufacturing a replacement would hide the problem and orphan the real file.
	/// </remarks>
	private static void EnsureTagSourceExists()
	{
		if (ForgeSettings.GetSourceReferences().Length > 0)
		{
			return;
		}

		const string path = ForgeSettings.DefaultSourcePath;

		if (ResourceLoader.Exists(path))
		{
			// Never overwrite. An empty setting does not mean an empty project: the file may have been authored before
			// it was attached, or the setting reset, and replacing it would destroy every tag in it.
			if (ResourceLoader.Load(path) is not ForgeTagsSource)
			{
				GD.PushError(
					$"'{path}' already exists and is not a Forge tag source. Move it aside, or point "
					+ $"'{ForgeSettings.SourcesSetting}' at a tag source yourself.");
				return;
			}

			ForgeSettings.SetSourceReferences([ForgeSettings.ToPreferredReference(path)]);
			GD.Print("Using the existing tag source at ", path);

			return;
		}

		var tagsSource = new ForgeTagsSource();
		Error error = ResourceSaver.Save(tagsSource, path);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to create tag source at {path}: {error}");
			return;
		}

		// UpdateFile registers the new file, and its UID, synchronously. Scan only queues a rescan, so the reference
		// taken below could still be a plain path - which then breaks the moment the file is moved.
		EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(path);
		ForgeSettings.SetSourceReferences([ForgeSettings.ToPreferredReference(path)]);

		GD.Print("Created tag source at ", path);
	}

	private static void OnResourcesReload(string[] resources)
	{
		ForgeTagsRegistry.InvalidateIfAny(resources);
	}

	private static void OnProjectSettingsChanged()
	{
		ForgeTagsRegistry.InvalidateIfSourcesChanged();
	}

	private static void OnResourceSaved(Resource resource)
	{
		if (resource is not ForgeAttributeSetDefinition)
		{
			return;
		}

		AttributeSetGenerationReport report = AttributeSetCodeGenerator.RegenerateAll();

		foreach (string error in report.Errors)
		{
			GD.PushWarning($"Forge attribute set generation: {error}");
		}

		if (report.GeneratedSets.Count > 0 || report.RemovedFiles.Count > 0)
		{
			GD.Print("Forge attribute sets regenerated. Build the project for the changes to take effect.");
		}
	}

	private void OnToolsMenuIdPressed(long id)
	{
		if (id == RepairToolItemId)
		{
			_repairDialog?.Open();
		}
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
		ForgeTagsRegistry.InvalidateIfAny(resources);

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
