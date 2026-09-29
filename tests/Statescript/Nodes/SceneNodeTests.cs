// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The Scene nodes' ownership of what they spawn, run in a real graph against a real scene tree.
/// </summary>
[TestSuite]
public class SceneNodeTests
{
	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void An_instance_that_ends_the_graph_as_it_arrives_is_freed_with_it()
	{
		var parent = new Node3D();
		Root.AddChild(parent);

		var template = new TestInstantiationHook();
		var scene = new PackedScene();
		scene.Pack(template);
		template.Free();

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable("scene", scene);
			graph.VariableDefinitions.DefineObjectVariable<Node>("parent", parent);

			var spawn = new Scene3DNode(InstantiateParentMode.Node, passOwnership: true);
			spawn.BindInput(SceneNodeBase.SceneInput, "scene");
			spawn.BindInput(SceneNodeBase.ParentNodeInput, "parent");
			graph.AddNode(spawn);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				spawn.InputPorts[Scene3DNode.InputPort]));

			var processor = new GraphProcessor(graph);

			// The instance ends the ability the graph runs for as it is handed its owner, before the node holds it.
			TestInstantiationHook.Instantiated = processor.StopGraph;
			processor.StartGraph();

			parent.GetChildren().Should().ContainSingle()
				.Which.IsQueuedForDeletion().Should().BeTrue("nothing else is left to free it");
		}
		finally
		{
			TestInstantiationHook.Instantiated = null;
			parent.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_instance_that_starts_the_graph_over_as_it_arrives_is_freed_with_its_run()
	{
		var parent = new Node3D();
		Root.AddChild(parent);

		var template = new TestInstantiationHook();
		var scene = new PackedScene();
		scene.Pack(template);
		template.Free();

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable("scene", scene);
			graph.VariableDefinitions.DefineObjectVariable<Node>("parent", parent);

			var spawn = new Scene3DNode(InstantiateParentMode.Node, passOwnership: true);
			spawn.BindInput(SceneNodeBase.SceneInput, "scene");
			spawn.BindInput(SceneNodeBase.ParentNodeInput, "parent");
			graph.AddNode(spawn);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				spawn.InputPorts[Scene3DNode.InputPort]));

			var processor = new GraphProcessor(graph);
			int completions = 0;

			// The first instance stops the graph as it is handed its owner, and the graph starts over before the node
			// holds it; the new run's instance arrives quietly.
			TestInstantiationHook.Instantiated = () =>
			{
				TestInstantiationHook.Instantiated = null;
				processor.StopGraph();
			};
			processor.OnGraphCompleted = () =>
			{
				if (++completions == 1)
				{
					processor.StartGraph();
				}
			};

			processor.StartGraph();

			Node[] instances = [.. parent.GetChildren()];
			instances.Should().HaveCount(2);
			instances[0].IsQueuedForDeletion().Should().BeTrue("nothing else is left to free the first run's");
			instances[1].IsQueuedForDeletion().Should().BeFalse("the new run keeps its own");
		}
		finally
		{
			TestInstantiationHook.Instantiated = null;
			parent.Free();
		}
	}
}
