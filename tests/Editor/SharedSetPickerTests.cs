// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using VariableScope = Gamesmiths.Forge.Statescript.VariableScope;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Shared variable bindings whose set the pickers do not list, as when the set was moved or deleted. The headless
/// editor has no file index, so here every set is unlisted while its file still loads.
/// </summary>
internal static class SharedSetPickerTests
{
	private const string SetPath = "user://forge_editor_tests_shared_set.tres";

	/// <summary>
	/// Opening the set dropdown refilled it and cleared the unlisted set's path, so choosing one of the variables still
	/// listed below removed the Set Variable node's output binding.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_set_variable_node_keeps_an_unlisted_set_when_its_list_refreshes(EditorTestContext context)
	{
		var node = new StatescriptNode
		{
			NodeId = "node_1",
			Title = "Set Variable",
			NodeType = StatescriptNodeType.Action,
			RuntimeTypeName = "Gamesmiths.Forge.Statescript.Nodes.Action.SetVariableNode",
			CustomData = new() { { "_output_scope", (int)VariableScope.Shared } },
		};

		node.PropertyBindings.Add(SharedBinding(StatescriptPropertyDirection.Output));

		CheckRefreshKeepsSet(context, node, StatescriptPropertyDirection.Output);
	}

	/// <summary>
	/// The same refresh left a Variable resolver pointing at no set once a variable was chosen.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_variable_resolver_keeps_an_unlisted_set_when_its_list_refreshes(EditorTestContext context)
	{
		var node = new StatescriptNode
		{
			NodeId = "node_1",
			Title = "Debug",
			NodeType = StatescriptNodeType.Action,
			RuntimeTypeName = typeof(DebugNode).FullName!,
		};

		node.PropertyBindings.Add(SharedBinding(StatescriptPropertyDirection.Input));

		CheckRefreshKeepsSet(context, node, StatescriptPropertyDirection.Input);
	}

	private static StatescriptNodeProperty SharedBinding(StatescriptPropertyDirection direction)
	{
		return new StatescriptNodeProperty
		{
			Direction = direction,
			Resolver = new VariableResolverResource
			{
				Scope = VariableScope.Shared,
				SharedVariableSetPath = SetPath,
				VariableName = "speed",
			},
		};
	}

	private static void CheckRefreshKeepsSet(
		EditorTestContext context,
		StatescriptNode node,
		StatescriptPropertyDirection direction)
	{
		var set = new ForgeSharedVariableSet();
		set.Variables.Add(new ForgeSharedVariableDefinition { VariableName = "speed" });
		ResourceSaver.Save(set, SetPath).Should().Be(Error.Ok);

		try
		{
			var graph = new StatescriptGraph { StatescriptName = "EditorTests" };
			graph.EnsureEntryNode();
			graph.Nodes.Add(node);
			context.Dock.OpenGraph(graph);

			OptionButton sets = FindAll<OptionButton>(context.Dock).Single(x => x.GetParent().GetChildren()
				.OfType<Label>()
				.Any(label => label.Text == "Set:"));
			sets.GetPopup().EmitSignal(Window.SignalName.AboutToPopup);

			OptionButton variables = FindAll<OptionButton>(context.Dock).Single(x =>
				x.HasMeta("is_shared_variable_dropdown"));
			List<string> names = [.. Enumerable.Range(0, variables.ItemCount).Select(variables.GetItemText)];
			names.Should().Contain("speed", "the unlisted set's variables stay on offer");

			variables.Selected = names.IndexOf("speed");
			variables.EmitSignal(OptionButton.SignalName.ItemSelected, (long)variables.Selected);

			node.PropertyBindings.Single(x => x.Direction == direction).Resolver.Should()
				.BeOfType<VariableResolverResource>().Which.SharedVariableSetPath.Should()
				.Be(SetPath, "choosing a variable keeps the set it belongs to");
		}
		finally
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SetPath));
		}
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
