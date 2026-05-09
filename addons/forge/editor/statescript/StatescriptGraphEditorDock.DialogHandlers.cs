// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphEditorDock
{
	private void OnFileMenuIdPressed(long id)
	{
		switch ((int)id)
		{
			case 0:
				ShowNewStatescriptDialog();
				break;

			case 1:
				ShowLoadStatescriptDialog();
				break;

			case 2:
				OnSavePressed();
				break;

			case 3:
				ShowSaveAsDialog();
				break;

			case 4:
				CloseCurrentTab();
				break;
		}
	}

	private void ShowNewStatescriptDialog()
	{
		_newStatescriptDialog?.QueueFree();

		_newStatescriptDialog = new AcceptDialog
		{
			Title = "Create Statescript",
			Size = new Vector2I(400, 140),
			Exclusive = true,
		};

		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var pathRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(pathRow);

		pathRow.AddChild(new Label { Text = "Path:", CustomMinimumSize = new Vector2(50, 0) });

		_newStatescriptPathEdit = new LineEdit
		{
			Text = "res://new_statescript.tres",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		pathRow.AddChild(_newStatescriptPathEdit);

		_newStatescriptDialog.AddChild(vBox);
		_newStatescriptDialog.Confirmed += OnNewStatescriptConfirmed;

		AddChild(_newStatescriptDialog);
		_newStatescriptDialog.PopupCentered();
	}

	private void OnNewStatescriptConfirmed()
	{
		if (_newStatescriptPathEdit is null)
		{
			return;
		}

		string path = _newStatescriptPathEdit.Text.Trim();
		if (string.IsNullOrEmpty(path))
		{
			return;
		}

		if (!path.EndsWith(".tres", System.StringComparison.OrdinalIgnoreCase))
		{
			path += ".tres";
		}

		var graph = new StatescriptGraph();
		graph.EnsureEntryNode();

		graph.StatescriptName = path.GetFile().GetBaseName();

		ResourceSaver.Save(graph, path);
		EditorInterface.Singleton.GetResourceFilesystem().Scan();

		graph = ResourceLoader.Load<StatescriptGraph>(path);
		if (graph is not null)
		{
			OpenGraph(graph);
		}

		_newStatescriptDialog?.QueueFree();
		_newStatescriptDialog = null;
	}

	private void ShowLoadStatescriptDialog()
	{
		var dialog = new EditorFileDialog
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Title = "Load Statescript File",
			Access = FileDialog.AccessEnum.Resources,
		};

		dialog.AddFilter("*.tres;StatescriptGraph");
		dialog.FileSelected += path =>
		{
			Resource? graph = ResourceLoader.Load(path);
			if (graph is StatescriptGraph statescriptGraph)
			{
				OpenGraph(statescriptGraph);
			}
			else
			{
				GD.PushWarning($"Failed to load StatescriptGraph from: {path}");
			}

			dialog.QueueFree();
		};

		dialog.Canceled += dialog.QueueFree;

		AddChild(dialog);
		dialog.PopupCentered(new Vector2I(700, 500));
	}

	private void ShowSaveAsDialog()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null)
		{
			return;
		}

		var dialog = new EditorFileDialog
		{
			FileMode = FileDialog.FileModeEnum.SaveFile,
			Title = "Save Statescript As",
			Access = FileDialog.AccessEnum.Resources,
		};

		dialog.AddFilter("*.tres", "Godot Resource");
		dialog.FileSelected += path =>
		{
			if (_graphEdit is not null)
			{
				graph.ScrollOffset = _graphEdit.ScrollOffset;
				graph.Zoom = _graphEdit.Zoom;
				SyncVisualNodePositionsToGraph();
				SyncConnectionsToCurrentGraph();
			}

			ResourceSaver.Save(graph, path);
			EditorInterface.Singleton.GetResourceFilesystem().Scan();
			GD.Print($"Statescript graph saved as: {path}");

			StatescriptGraph? savedGraph = ResourceLoader.Load<StatescriptGraph>(path);
			if (savedGraph is not null)
			{
				OpenGraph(savedGraph);
			}

			dialog.QueueFree();
		};

		dialog.Canceled += dialog.QueueFree;

		AddChild(dialog);
		dialog.PopupCentered(new Vector2I(700, 500));
	}

	private void OnSavePressed()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		graph.ScrollOffset = _graphEdit.ScrollOffset;
		graph.Zoom = _graphEdit.Zoom;
		SyncVisualNodePositionsToGraph();
		SyncConnectionsToCurrentGraph();

		if (string.IsNullOrEmpty(graph.ResourcePath))
		{
			ShowSaveAsDialog();
			return;
		}

		SaveGraphResource(graph);
		GD.Print($"Statescript graph saved: {graph.ResourcePath}");
	}

	private void OnGraphEditPopupRequest(Vector2 atPosition)
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null)
		{
			return;
		}

		ClearPendingConnection();

		Vector2 graphPosition = (_graphEdit.ScrollOffset + atPosition) / _graphEdit.Zoom;
		var screenPosition = (Vector2I)(_graphEdit.GetScreenPosition() + atPosition);

		_addNodeDialog.ShowAtPosition(graphPosition, screenPosition);
	}

	private void OnConnectionToEmpty(StringName fromNode, long fromPort, Vector2 releasePosition)
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null)
		{
			return;
		}

		_pendingConnectionNode = fromNode;
		_pendingConnectionPort = (int)fromPort;
		_pendingConnectionIsOutput = true;

		Vector2 graphPosition = (_graphEdit.ScrollOffset + releasePosition) / _graphEdit.Zoom;
		var screenPosition = (Vector2I)(_graphEdit.GetScreenPosition() + releasePosition);

		_addNodeDialog.ShowAtPosition(graphPosition, screenPosition);
	}

	private void OnConnectionFromEmpty(StringName toNode, long toPort, Vector2 releasePosition)
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null)
		{
			return;
		}

		_pendingConnectionNode = toNode;
		_pendingConnectionPort = (int)toPort;
		_pendingConnectionIsOutput = false;

		Vector2 graphPosition = (_graphEdit.ScrollOffset + releasePosition) / _graphEdit.Zoom;
		var screenPosition = (Vector2I)(_graphEdit.GetScreenPosition() + releasePosition);

		_addNodeDialog.ShowAtPosition(graphPosition, screenPosition);
	}

	private void OnDialogNodeCreationRequested(
		StatescriptNodeDiscovery.NodeTypeInfo? typeInfo,
		StatescriptNodeType nodeType,
		Vector2 position)
	{
		string newNodeId;

		if (typeInfo is not null)
		{
			newNodeId = AddNodeAtPosition(nodeType, typeInfo.DisplayName, typeInfo.RuntimeTypeName, position);
		}
		else
		{
			newNodeId = AddNodeAtPosition(StatescriptNodeType.Exit, "Exit", string.Empty, position);
		}

		if (_pendingConnectionNode is not null && _graphEdit is not null)
		{
			if (_pendingConnectionIsOutput)
			{
				int inputPort = FindFirstEnabledInputPort(newNodeId);
				if (inputPort >= 0)
				{
					OnConnectionRequest(
						_pendingConnectionNode,
						_pendingConnectionPort,
						newNodeId,
						inputPort);
				}
			}
			else
			{
				int outputPort = FindFirstEnabledOutputPort(newNodeId);
				if (outputPort >= 0)
				{
					OnConnectionRequest(
						newNodeId,
						outputPort,
						_pendingConnectionNode,
						_pendingConnectionPort);
				}
			}

			ClearPendingConnection();
		}
	}

	private void OnDialogCanceled()
	{
		ClearPendingConnection();
	}

	private void ClearPendingConnection()
	{
		_pendingConnectionNode = null;
		_pendingConnectionPort = 0;
		_pendingConnectionIsOutput = false;
	}

	private void OnAddNodeButtonPressed()
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null || _addNodeButton is null)
		{
			return;
		}

		ClearPendingConnection();

		var screenPosition = (Vector2I)(_addNodeButton.GetScreenPosition() + new Vector2(0, _addNodeButton.Size.Y));

		Vector2 centerPosition = (_graphEdit.ScrollOffset + (_graphEdit.Size / 2)) / _graphEdit.Zoom;

		_addNodeDialog.ShowAtPosition(centerPosition, screenPosition);
	}
}
#endif
