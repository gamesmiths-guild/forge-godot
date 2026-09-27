// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// The Restart On Retrigger checkbox. A node's settings are drawn one of three ways - the default rows, a standard
/// editor's Settings section, or a custom editor's own sections - and the checkbox has to reach every node that can
/// restart through whichever of them draws it.
/// </summary>
internal static class RestartOnRetriggerTests
{
	private static readonly NodeConfigParam _setting = NodeConfigParam.RestartOnRetrigger;

	/// <summary>
	/// Adds every state node to one graph and checks each for the checkbox. A node that can restart but is drawn by an
	/// editor that leaves the checkbox out cannot be told to, and one that cannot restart would be offered a choice
	/// that changes nothing.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Every_state_node_that_can_restart_shows_the_setting_and_no_other_does(
		EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		var mismatched = new List<string>();

		foreach (StatescriptNodeDiscovery.NodeTypeInfo info in StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.Where(x => x.NodeType == StatescriptNodeType.State))
		{
			string nodeId = context.Dock.TestOnlyAddNode(info.DisplayName, info.RuntimeTypeName);

			if ((FindSetting(context, graph, nodeId) is not null) != info.CanRestartOnRetrigger)
			{
				mismatched.Add(info.RuntimeTypeName);
			}
		}

		mismatched.Should().BeEmpty("a state node shows the setting exactly when it can restart");
	}

	/// <summary>
	/// A node no custom editor draws: its checkbox comes from the node's own Settings section.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Ticking_the_setting_on_a_node_with_default_rows_is_one_undo_step(EditorTestContext context)
	{
		CheckTickingIsOneUndoStep(context, "Gamesmiths.Forge.Statescript.Nodes.State.TimerNode");
	}

	/// <summary>
	/// A node drawn by a standard editor: its checkbox joins the editor's own settings.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Ticking_the_setting_on_a_node_with_a_standard_editor_is_one_undo_step(
		EditorTestContext context)
	{
		CheckTickingIsOneUndoStep(context, typeof(MoveTo3DNode).FullName!);
	}

	/// <summary>
	/// A node drawn by a custom editor: its checkbox comes from the node's own Settings section, above the editor's.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Ticking_the_setting_on_a_node_with_a_custom_editor_is_one_undo_step(EditorTestContext context)
	{
		CheckTickingIsOneUndoStep(context, "Gamesmiths.Forge.Statescript.Nodes.State.EffectNode");
	}

	private static void CheckTickingIsOneUndoStep(EditorTestContext context, string runtimeTypeName)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string nodeId = context.Dock.TestOnlyAddNode("Restartable", runtimeTypeName);
		StatescriptNode node = graph.Nodes.Single(x => x.NodeId == nodeId);

		FindSetting(context, graph, nodeId)!.ButtonPressed = true;

		node.CustomData[_setting.Key].AsBool().Should().BeTrue(
			"ticking the box stores the choice under the constructor parameter the graph builder reads");

		UndoRedo history = context.HistoryFor(graph);
		history.Undo();

		node.CustomData.Should().NotContainKey(_setting.Key, "undo takes the choice back");
		FindSetting(context, graph, nodeId)!.ButtonPressed.Should().BeFalse("the box shows what undo restored");

		history.Redo();

		node.CustomData[_setting.Key].AsBool().Should().BeTrue("redo stores the choice again");
		FindSetting(context, graph, nodeId)!.ButtonPressed.Should().BeTrue("the box shows what redo restored");
	}

	private static CheckBox? FindSetting(EditorTestContext context, StatescriptGraph graph, string nodeId)
	{
		StatescriptGraphNode visual = context.Dock.TestOnlyNodeVisual(graph, nodeId)!;
		return FindAll<CheckBox>(visual).SingleOrDefault(x => x.Text == _setting.Label);
	}

	private static IEnumerable<T> FindAll<T>(Node root)
		where T : Node
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is T match)
			{
				yield return match;
			}

			foreach (T nested in FindAll<T>(child))
			{
				yield return nested;
			}
		}
	}
}
#endif
