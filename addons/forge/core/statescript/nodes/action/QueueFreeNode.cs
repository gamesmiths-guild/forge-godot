// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that frees a node from the scene.
/// </summary>
/// <remarks>
/// The free is queued rather than immediate, which is what makes it safe to call on something mid-signal or
/// mid-physics. A node that has already been freed is ignored rather than reported, since a graph reaching this twice,
/// or racing something else that frees the same node, is ordinary rather than a mistake.
/// </remarks>
[StatescriptCategory("Scene")]
public sealed class QueueFreeNode : ActionNode
{
	/// <summary>
	/// Input property index for the node to free.
	/// </summary>
	public const byte NodeInput = 0;

	/// <inheritdoc/>
	public override string Description => "Frees a node from the scene.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Node", typeof(Node)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (graphContext.TryResolveObject(InputProperties[NodeInput].BoundName, out Node? node)
			&& node is not null
			&& GodotObject.IsInstanceValid(node))
		{
			node.QueueFree();
		}
	}
}
