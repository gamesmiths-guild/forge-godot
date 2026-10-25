// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using GdUnit4;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// Graph snapshots across every state node the editor lists, the plugin's own and the core ones alike. A snapshot
/// copies node contexts member by member, so a context type that changes a collection in place has to give each copy
/// one of its own, or the run after a restore would write into the snapshot it came from.
/// </summary>
[TestSuite]
public class SnapshotConformanceTests
{
	/// <summary>
	/// Gets one case per discovered state node type.
	/// </summary>
	public static IEnumerable<object[]> DiscoveredStateNodes =>
		StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.Where(info => info.NodeType == StatescriptNodeType.State)
			.Select(info => new object[] { info.RuntimeTypeName })
			.OrderBy(data => (string)data[0], StringComparer.Ordinal);

	[TestCase]
	[DataPoint(nameof(DiscoveredStateNodes))]
	[RequireGodotRuntime]
	public void A_restored_context_holds_the_same_values_and_collections_of_its_own(string runtimeTypeName)
	{
		ForgeNode node = StatescriptNodeFactory.Create(
			StatescriptNodeFactory.ResolveType(runtimeTypeName)!,
			[]);

		// Kept running beside the node, so the run and its contexts outlast a node that ends as it starts: with nothing
		// to watch, a condition monitor never ends.
		var keeper = new ConditionMonitorNode();
		var graph = new ForgeGraph();
		graph.AddNode(keeper);
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keeper.InputPorts[StateNode<StateNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		StateNodeContext taken = processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID);
		GraphSnapshot snapshot = processor.CaptureSnapshot();
		processor.RestoreSnapshot(snapshot);
		StateNodeContext restored = processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID);

		restored.Should().NotBeSameAs(taken).And.BeOfType(taken.GetType());

		foreach (PropertyInfo property in ContextProperties(taken.GetType()))
		{
			object? before = property.GetValue(taken);
			object? after = property.GetValue(restored);

			if (IsChangedInPlace(property.PropertyType) && before is not null)
			{
				after.Should().NotBeSameAs(
					before,
					$"{property.DeclaringType!.Name}.{property.Name} is a collection the node changes in place");
				((IEnumerable)after!).Cast<object>().Should().Equal(((IEnumerable)before).Cast<object>());
			}
			else if (property.PropertyType.IsValueType)
			{
				after.Should().Be(before, $"{property.Name} is copied as it was");
			}
			else
			{
				after.Should().BeSameAs(before, $"{property.Name} still points at the same object");
			}
		}

		processor.StopGraph();
	}

	private static IEnumerable<PropertyInfo> ContextProperties(Type type)
	{
		for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
		{
			foreach (PropertyInfo property in current.GetProperties(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (property.GetIndexParameters().Length == 0
					&& !typeof(Delegate).IsAssignableFrom(property.PropertyType))
				{
					yield return property;
				}
			}
		}
	}

	// Arrays are replaced whole rather than written into, and read-only views are never written through.
	private static bool IsChangedInPlace(Type type)
	{
		return !type.IsArray
			&& !type.IsInterface
			&& Array.Exists(
				type.GetInterfaces(),
				x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
	}
}
#endif
