// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;
using NodeNetworkKind = Gamesmiths.Forge.Statescript.NodeNetworkKind;
using NodeRegion = Gamesmiths.Forge.Statescript.NodeRegion;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

// Regions are frames around their nodes, made from the graph's resource whenever it changes, so the resource is the one
// place they live. Each node's badge shows the kind it has in this graph, worked out by building the graph.
public partial class StatescriptGraphEditorDock
{
	private const string RegionFramePrefix = "__region_";

	private const int ClientOnlyRegionId = 0;

	private const int ServerOnlyRegionId = 1;

	private const int LeaveRegionId = 2;

	private static readonly Color _clientOnlyTint = new(0.25f, 0.55f, 1f, 0.3f);

	private static readonly Color _serverOnlyTint = new(0.62f, 0.32f, 0.95f, 0.3f);

	private MenuButton? _regionMenuButton;

	private PopupMenu? _regionMenuPopup;

	private bool _networkRefreshPending;

	private static GodotCollections.Array<StatescriptRegion> CopyRegions(
		GodotCollections.Array<StatescriptRegion> regions)
	{
		var copy = new GodotCollections.Array<StatescriptRegion>();

		foreach (StatescriptRegion region in regions)
		{
			copy.Add(new StatescriptRegion { Kind = region.Kind, NodeIds = [.. region.NodeIds] });
		}

		return copy;
	}

	// Takes the nodes out of every region but the one kept, and drops the regions left empty.
	private static void LeaveRegions(
		GodotCollections.Array<StatescriptRegion> regions,
		List<string> nodeIds,
		StatescriptRegion? kept = null)
	{
		for (int i = regions.Count - 1; i >= 0; i--)
		{
			StatescriptRegion region = regions[i];

			if (region == kept)
			{
				continue;
			}

			for (int j = region.NodeIds.Count - 1; j >= 0; j--)
			{
				if (nodeIds.Contains(region.NodeIds[j]))
				{
					region.NodeIds.RemoveAt(j);
				}
			}

			if (region.NodeIds.Count == 0)
			{
				regions.RemoveAt(i);
			}
		}
	}

	// The regions as deleting these frames and nodes leaves them: a deleted frame's region goes and its nodes stay, and
	// a deleted node leaves its region, which goes once empty, so a later node given its id doesn't join it. Null when
	// nothing changes.
	private static GodotCollections.Array<StatescriptRegion>? RegionsAfterDeleting(
		StatescriptGraph graph,
		GodotCollections.Array<StringName> names,
		List<string> deletedIds)
	{
		var deleted = new HashSet<int>();

		foreach (StringName name in names)
		{
			if (TryGetRegionIndex(name, out int index) && index < graph.Regions.Count)
			{
				deleted.Add(index);
			}
		}

		if (deleted.Count == 0 && !graph.Regions.Any(x => x.NodeIds.Any(deletedIds.Contains)))
		{
			return null;
		}

		GodotCollections.Array<StatescriptRegion> regions = CopyRegions(graph.Regions);

		for (int i = regions.Count - 1; i >= 0; i--)
		{
			if (deleted.Contains(i))
			{
				regions.RemoveAt(i);
			}
		}

		LeaveRegions(regions, deletedIds);
		return regions;
	}

	private static bool TryGetRegionIndex(StringName frame, out int index)
	{
		string name = frame.ToString();
		index = -1;

		return name.StartsWith(RegionFramePrefix, StringComparison.Ordinal)
			&& int.TryParse(name.AsSpan(RegionFramePrefix.Length), out index);
	}

	private void AddRegionMenu(HBoxContainer menuHBox, int index)
	{
		_regionMenuButton = new MenuButton
		{
			Text = "Region",
			Flat = false,
			SwitchOnHover = true,
			ThemeTypeVariation = "FlatMenuButton",
			TooltipText = "Marks the selected nodes to run differently in a networked game.",
		};

		_regionMenuPopup = _regionMenuButton.GetPopup();
		_regionMenuPopup.AddItem("Client Only", ClientOnlyRegionId);
		const string clientOnly =
			"Never sent to clients: every machine runs these nodes itself, a client from the synced state or the entry "
			+ "that leads into them.";
		_regionMenuPopup.SetItemTooltip(ClientOnlyRegionId, clientOnly);
		_regionMenuPopup.AddItem("Server Only", ServerOnlyRegionId);
		_regionMenuPopup.SetItemTooltip(ServerOnlyRegionId, "Never runs on a client, whatever the nodes' kinds.");
		_regionMenuPopup.AddSeparator();
		_regionMenuPopup.AddItem("Leave Region", LeaveRegionId);

		menuHBox.AddChild(_regionMenuButton);
		menuHBox.MoveChild(_regionMenuButton, index);
	}

	private void OnRegionMenuIdPressed(long id)
	{
		switch (id)
		{
			case ClientOnlyRegionId:
				MakeRegionOfSelection(NodeRegion.ClientOnly);
				break;

			case ServerOnlyRegionId:
				MakeRegionOfSelection(NodeRegion.ServerOnly);
				break;

			case LeaveRegionId:
				LeaveRegionsWithSelection();
				break;
		}
	}

	private List<string> SelectedNodeIds()
	{
		var ids = new List<string>();

		if (_graphEdit is null)
		{
			return ids;
		}

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode { Selected: true, NodeResource: StatescriptNode node }
				&& node.NodeType != StatescriptNodeType.Entry)
			{
				ids.Add(node.NodeId);
			}
		}

		return ids;
	}

	private void MakeRegionOfSelection(NodeRegion kind)
	{
		StatescriptGraph? graph = CurrentGraph;
		List<string> selected = SelectedNodeIds();

		if (graph is null || selected.Count == 0)
		{
			return;
		}

		GodotCollections.Array<StatescriptRegion> regions = CopyRegions(graph.Regions);
		LeaveRegions(regions, selected);
		regions.Add(new StatescriptRegion { Kind = kind, NodeIds = [.. selected] });

		RecordRegions(
			graph,
			regions,
			kind == NodeRegion.ClientOnly ? "Make Client-Only Region" : "Make Server-Only Region");
	}

	private void LeaveRegionsWithSelection()
	{
		StatescriptGraph? graph = CurrentGraph;
		List<string> selected = SelectedNodeIds();

		if (graph is null || selected.Count == 0)
		{
			return;
		}

		GodotCollections.Array<StatescriptRegion> regions = CopyRegions(graph.Regions);
		LeaveRegions(regions, selected);
		RecordRegions(graph, regions, "Leave Statescript Region");
	}

	// Nodes dropped onto a frame join its region and leave any other.
	private void OnGraphElementsLinkedToFrameRequest(GodotCollections.Array elements, StringName frame)
	{
		StatescriptGraph? graph = CurrentGraph;

		if (graph is null || !TryGetRegionIndex(frame, out int index) || index >= graph.Regions.Count)
		{
			return;
		}

		GodotCollections.Array<StatescriptRegion> regions = CopyRegions(graph.Regions);
		StatescriptRegion target = regions[index];
		var joining = new List<string>();

		foreach (Variant element in elements)
		{
			string nodeId = element.AsStringName().ToString();

			if (_graphEdit?.GetNodeOrNull(nodeId) is StatescriptGraphNode { NodeResource: StatescriptNode node }
				&& node.NodeType != StatescriptNodeType.Entry
				&& !target.NodeIds.Contains(nodeId))
			{
				joining.Add(nodeId);
			}
		}

		if (joining.Count == 0)
		{
			return;
		}

		LeaveRegions(regions, joining, kept: target);
		target.NodeIds.AddRange(joining);
		RecordRegions(graph, regions, "Join Statescript Region");
	}

	private void RecordRegions(StatescriptGraph graph, GodotCollections.Array<StatescriptRegion> regions, string name)
	{
		GodotCollections.Array<StatescriptRegion> before = CopyRegions(graph.Regions);

		EditorUndoRedoUtils.Record(
			_undoRedo,
			name,
			graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoSetRegions, graph, regions);
				undo.AddUndoMethod(this, MethodName.DoSetRegions, graph, before);
			},
			execute: true,
			fallback: () => DoSetRegions(graph, regions));
	}

	private void DoSetRegions(StatescriptGraph graph, GodotCollections.Array<StatescriptRegion> regions)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		graph.Regions = CopyRegions(regions);
		graph.EmitChanged();

		if (CurrentGraph == graph)
		{
			RefreshRegionFrames(graph);
		}
	}

	private void RefreshRegionFrames(StatescriptGraph graph)
	{
		if (_graphEdit is null)
		{
			return;
		}

		RemoveRegionFrames();

		for (int i = 0; i < graph.Regions.Count; i++)
		{
			StatescriptRegion region = graph.Regions[i];

			// A region saved with only deleted nodes in it has nothing to frame.
			if (region.Kind == NodeRegion.None
				|| !region.NodeIds.Any(x => _graphEdit.GetNodeOrNull(x) is StatescriptGraphNode))
			{
				continue;
			}

			bool clientOnly = region.Kind == NodeRegion.ClientOnly;

			var frame = new GraphFrame
			{
				Name = $"{RegionFramePrefix}{i}",
				Title = clientOnly ? "Client Only" : "Server Only",
				AutoshrinkEnabled = true,
				TintColorEnabled = true,
				TintColor = clientOnly ? _clientOnlyTint : _serverOnlyTint,
				TooltipText = clientOnly
					? "Client only: never sent to clients. Every machine runs these nodes itself, a client from the "
						+ "synced state or the entry that leads into them."
					: "Server only: these nodes never run on a client.",
			};

			_graphEdit.AddChild(frame);

			foreach (string nodeId in region.NodeIds)
			{
				if (_graphEdit.GetNodeOrNull(nodeId) is StatescriptGraphNode)
				{
					_graphEdit.AttachGraphElementToFrame(nodeId, frame.Name);
				}
			}
		}
	}

	private void RemoveRegionFrames()
	{
		if (_graphEdit is null)
		{
			return;
		}

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is not GraphFrame frame)
			{
				continue;
			}

			foreach (StringName element in _graphEdit.GetAttachedNodesOfFrame(frame.Name))
			{
				_graphEdit.DetachGraphElementFromFrame(element);
			}

			_graphEdit.RemoveChild(frame);
			frame.QueueFree();
		}
	}

	// Coalesces the changes of one frame into one refresh.
	private void ScheduleNetworkRefresh()
	{
		if (_networkRefreshPending)
		{
			return;
		}

		_networkRefreshPending = true;
		CallDeferred(MethodName.RefreshNetworkView);
	}

	private void RefreshNetworkView()
	{
		_networkRefreshPending = false;

		if (CurrentGraph is StatescriptGraph graph)
		{
			RefreshRegionFrames(graph);
			RefreshNetworkKinds(graph);
		}
	}

	private void RefreshNetworkKinds(StatescriptGraph graph)
	{
		if (_graphEdit is null)
		{
			return;
		}

		Dictionary<string, NodeNetworkKind> kinds;

		try
		{
			kinds = StatescriptGraphBuilder.NetworkKinds(graph);
		}
		catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
		{
			// A graph that doesn't build yet, such as one with a setting its node can't take, keeps the kinds its node
			// types declare.
			return;
		}

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode { NodeResource: StatescriptNode node } graphNode
				&& kinds.TryGetValue(node.NodeId, out NodeNetworkKind kind))
			{
				graphNode.ShowNetworkKind(kind);
			}
		}
	}
}
#endif
