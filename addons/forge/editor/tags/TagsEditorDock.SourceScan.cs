// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// The dock's on-demand search for tag sources the project is not reading yet.
/// </summary>
public partial class TagsEditorDock
{
	private static readonly Vector2I _scanDialogSize = new(560, 250);

	private readonly Dictionary<TreeItem, string> _scanRows = [];

	private ConfirmationDialog? _scanDialog;
	private Tree? _scanTree;
	private Label? _scanSummary;

	private void BuildScanDialog()
	{
		_scanDialog = new ConfirmationDialog
		{
			Title = "Find Tag Sources",
			OkButtonText = "Add Selected",
		};

		var vBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		_scanDialog.AddChild(vBox);

		_scanSummary = new Label
		{
			// A fixed height keeps the dialog's minimum size predictable. An autowrapping label with none reports a
			// minimum height computed against a near-zero width, which is what makes a popup open full-screen tall.
			CustomMinimumSize = new Vector2(0, 28),
		};

		vBox.AddChild(_scanSummary);

		_scanTree = new Tree
		{
			HideRoot = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		vBox.AddChild(_scanTree);

		AddChild(_scanDialog);
	}

	private void OnFindSourcesPressed()
	{
		if (_scanDialog is null || _scanTree is null || _scanSummary is null)
		{
			return;
		}

		// The index this reads is built by the editor's own scan. Searching mid-scan would quietly report nothing
		// found, which reads exactly like "this project has no tag sources".
		if (EditorInterface.Singleton.GetResourceFilesystem().IsScanning())
		{
			GD.PushWarning("Godot is still scanning the project. Try Find Sources again once it finishes.");
			return;
		}

		var stopwatch = Stopwatch.StartNew();
		List<string> found = TagsSourceScanner.FindTagSources();
		stopwatch.Stop();

		var registered = new HashSet<string>(
			ForgeSettings.GetSourceReferences().Select(ForgeSettings.ResolveReference),
			StringComparer.OrdinalIgnoreCase);

		_scanRows.Clear();
		_scanTree.Clear();

		TreeItem root = _scanTree.CreateItem();
		int unregisteredCount = 0;

		foreach (string path in found)
		{
			bool alreadyRegistered = registered.Contains(path);

			TreeItem item = _scanTree.CreateItem(root);
			item.SetCellMode(0, TreeItem.TreeCellMode.Check);
			item.SetText(0, alreadyRegistered ? $"{path}  (already a source)" : path);
			item.SetChecked(0, true);
			item.SetEditable(0, !alreadyRegistered);

			if (alreadyRegistered)
			{
				item.SetCustomColor(0, Color.FromHtml("8A8A8A"));
			}
			else
			{
				_scanRows[item] = path;
				unregisteredCount++;
			}
		}

		_scanSummary.Text = unregisteredCount == 0
			? $"Found {found.Count} tag source(s); all of them are already in use."
			: $"Found {unregisteredCount} tag source(s) this project is not reading yet.";

		_scanDialog.GetOkButton().Visible = unregisteredCount > 0;
		_scanDialog.CancelButtonText = unregisteredCount > 0 ? "Cancel" : "Close";

		GD.Print(
			$"Scanned for tag sources in {stopwatch.ElapsedMilliseconds} ms, found {found.Count}.");

		_scanDialog.PopupCentered(_scanDialogSize);
	}

	private void OnScanConfirmed()
	{
		foreach (KeyValuePair<TreeItem, string> row in _scanRows)
		{
			if (IsInstanceValid(row.Key) && row.Key.IsChecked(0))
			{
				AddSourceReference(row.Value);
			}
		}

		_scanRows.Clear();
	}
}
#endif
