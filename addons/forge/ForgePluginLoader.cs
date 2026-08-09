// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Editor;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Editor.Cues;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgePluginLoader : EditorPlugin
{
	private const string AutoloadPath = "uid://ba8fquhtwu5mu";
	private const string RepairToolItemText = "Repair assets tags";
	private const int RepairToolItemId = 0;

	private static readonly Vector2I _repairDialogSize = new(700, 300);

	private TagSourceEditingController? _tagEditingController;
	private TagsEditorDock? _tagsEditorDock;
	private TagsSourceInspectorPlugin? _tagsSourceInspectorPlugin;
	private TagContainerInspectorPlugin? _tagContainerInspectorPlugin;
	private QueryExpressionInspectorPlugin? _queryExpressionInspectorPlugin;
	private TagInspectorPlugin? _tagInspectorPlugin;
	private AttributeSetInspectorPlugin? _attributeSetInspectorPlugin;
	private CueHandlerInspectorPlugin? _cueHandlerInspectorPlugin;
	private AttributeEditorPlugin? _attributeEditorPlugin;
	private SharedVariableSetInspectorPlugin? _sharedVariableSetInspectorPlugin;
	private StatescriptGraphEditorDock? _statescriptGraphEditorDock;

	private ConfirmationDialog? _repairConfirmDialog;
	private PopupMenu? _toolsMenu;

	private EditorFileSystem? _fileSystem;
	private Callable _resourcesReimportedCallable;
	private Callable _resourcesReloadCallable;
	private Callable _toolsMenuIdPressedCallable;

	public override void _EnterTree()
	{
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
		_sharedVariableSetInspectorPlugin = new SharedVariableSetInspectorPlugin();
		_sharedVariableSetInspectorPlugin.SetUndoRedo(GetUndoRedo());
		AddInspectorPlugin(_sharedVariableSetInspectorPlugin);

		_statescriptGraphEditorDock = new StatescriptGraphEditorDock();
		_statescriptGraphEditorDock.SetUndoRedo(GetUndoRedo());
		AddDock(_statescriptGraphEditorDock);

		_repairConfirmDialog = new ConfirmationDialog
		{
			Title = "Repair Assets Tags",
			OkButtonText = "Repair",
		};

		_repairConfirmDialog.Confirmed += OnRepairConfirmed;
		AddChild(_repairConfirmDialog);

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

		Validation.Enabled = true;
	}

	public override void _ExitTree()
	{
		ProjectSettings.SettingsChanged -= OnProjectSettingsChanged;

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

		if (_repairConfirmDialog is not null)
		{
			_repairConfirmDialog.Confirmed -= OnRepairConfirmed;
			RemoveChild(_repairConfirmDialog);
			_repairConfirmDialog.QueueFree();
			_repairConfirmDialog = null;
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

		var tagsSource = new ForgeTagsSource();
		Error error = ResourceSaver.Save(tagsSource, ForgeSettings.DefaultSourcePath);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to create tag source at {ForgeSettings.DefaultSourcePath}: {error}");
			return;
		}

		// The scan has to happen before the reference is taken: GetResourceUid has nothing to return for a file the
		// filesystem has not indexed yet, which would quietly store a path instead of a UID.
		EditorInterface.Singleton.GetResourceFilesystem().Scan();
		ForgeSettings.SetSourceReferences([ForgeSettings.ToPreferredReference(ForgeSettings.DefaultSourcePath)]);

		GD.Print("Created tag source at ", ForgeSettings.DefaultSourcePath);
	}

	private static string BuildRepairPreview(List<AssetRepairTool.RepairFinding> findings)
	{
		const int MaxListedFindings = 40;

		const char LineBreak = '\n';

		var builder = new StringBuilder();
		int assetCount = findings.Select(finding => finding.AssetPath).Distinct().Count();

		string summary =
			$"{findings.Count} tag reference(s) across {assetCount} asset(s) do not resolve against the "
			+ "project's registered tags.";

		builder.Append(summary)
			.Append(LineBreak)
			.Append(LineBreak)
			.Append("Repairing removes them and saves the affected assets. This cannot be undone.")
			.Append(LineBreak)
			.Append(LineBreak);

		foreach (IGrouping<string, AssetRepairTool.RepairFinding> asset in findings
			.Take(MaxListedFindings)
			.GroupBy(finding => finding.AssetPath))
		{
			builder.Append(asset.Key).Append(LineBreak);

			foreach (AssetRepairTool.RepairFinding finding in asset)
			{
				builder.Append(CultureInfo.InvariantCulture, $"    {finding.Location}: {finding.Tag}")
					.Append(LineBreak);
			}
		}

		if (findings.Count > MaxListedFindings)
		{
			builder.Append(CultureInfo.InvariantCulture, $"    ... and {findings.Count - MaxListedFindings} more.")
				.Append(LineBreak);
		}

		return builder.ToString();
	}

	private static void OnResourcesReload(string[] resources)
	{
		ForgeTagsRegistry.InvalidateIfAny(resources);
	}

	private static void OnProjectSettingsChanged()
	{
		ForgeTagsRegistry.InvalidateIfSourcesChanged();
	}

	private static void OnRepairConfirmed()
	{
		List<AssetRepairTool.RepairFinding> repaired = AssetRepairTool.Apply();

		GD.Print($"Repaired {repaired.Count} tag reference(s).");
	}

	private void OnToolsMenuIdPressed(long id)
	{
		if (id == RepairToolItemId)
		{
			CallAssetRepairTool();
		}
	}

	private void CallAssetRepairTool()
	{
		if (_repairConfirmDialog is null)
		{
			return;
		}

		// Scan first and show what would change: this rewrites every scene in the project, so it must never be a single
		// unconfirmed click.
		List<AssetRepairTool.RepairFinding> findings = AssetRepairTool.Scan();

		if (findings.Count == 0)
		{
			_repairConfirmDialog.DialogText =
				"Every tag reference in this project resolves. Nothing to repair.";
			_repairConfirmDialog.GetOkButton().Visible = false;
			_repairConfirmDialog.CancelButtonText = "Close";
			_repairConfirmDialog.PopupCentered(_repairDialogSize);

			return;
		}

		_repairConfirmDialog.DialogText = BuildRepairPreview(findings);
		_repairConfirmDialog.GetOkButton().Visible = true;
		_repairConfirmDialog.CancelButtonText = "Cancel";

		// An explicit size is required, not just polite: MinSize is a floor, so a long findings list would otherwise
		// grow the dialog past the screen and push its own buttons out of reach.
		_repairConfirmDialog.PopupCentered(_repairDialogSize);
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
