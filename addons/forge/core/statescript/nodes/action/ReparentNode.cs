// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that moves a node to a new parent in the scene tree.
/// </summary>
/// <remarks>
/// <para>This is what makes a spawned thing stick to something: a shield effect parented onto the character it
/// protects follows them without a graph updating its position every frame, and a picked-up item parented onto a hand
/// marker is carried by the animation rather than by code. Dropping it is the same node pointed back at the level.
/// </para>
/// <para>Both operands are required. Unlike the interop nodes, there is no owner fallback: "reparent me" and "reparent
/// onto me" are different questions and neither is the obvious reading of an empty row, so the node says which node
/// moves and which node it moves under.</para>
/// <para>Godot rejects a reparent that would detach the tree or make a cycle, and it reports it as an error rather
/// than a warning. The three ways that happens - a node with no parent, a node moved onto itself, and a node moved
/// under its own descendant - are checked here instead, so a mis-authored graph reads as an authoring warning.</para>
/// </remarks>
/// <param name="keepGlobalTransform">Whether the node stays where it is in the world. Off makes it keep its position
/// relative to its parent instead, which is what attaching to a hand or a socket marker wants.</param>
[StatescriptCategory("Scene")]
public sealed class ReparentNode(bool keepGlobalTransform = true) : ActionNode
{
	/// <summary>
	/// Input property index for the node to move.
	/// </summary>
	public const byte NodeInput = 0;

	/// <summary>
	/// Input property index for the node to move it under.
	/// </summary>
	public const byte NewParentInput = 1;

	private readonly bool _keepGlobalTransform = keepGlobalTransform;
	private readonly HashSet<string> _warnings = [];

	/// <inheritdoc/>
	public override string Description => "Moves a node to a new parent in the scene tree.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Node", typeof(Node)));
		inputProperties.Add(new InputProperty("New Parent", typeof(Node)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!TryResolveNode(graphContext, InputProperties[NodeInput].BoundName, "node to move", out Node? node)
			|| !TryResolveNode(
				graphContext,
				InputProperties[NewParentInput].BoundName,
				"new parent",
				out Node? newParent))
		{
			return;
		}

		if (node.GetParent() is null)
		{
			WarnOnce(
				"no-parent",
				$"cannot move [{node.Name}], which has no parent to be moved out of. The reparent was skipped.");

			return;
		}

		// Both spellings of a cycle: a node cannot be its own parent, and it cannot go under something it already
		// contains, which would take the whole subtree out of the tree with it.
		if (node == newParent || node.IsAncestorOf(newParent))
		{
			WarnOnce(
				"cycle",
				$"cannot move [{node.Name}] under [{newParent.Name}], which is inside it. The reparent was skipped.");

			return;
		}

		node.Reparent(newParent, _keepGlobalTransform);
	}

	private bool TryResolveNode(
		GraphContext graphContext,
		StringKey boundName,
		string what,
		[NotNullWhen(true)] out Node? node)
	{
		if (graphContext.TryResolveObject(boundName, out node)
			&& node is not null
			&& GodotObject.IsInstanceValid(node))
		{
			return true;
		}

		WarnOnce(what, $"resolved no {what}. The reparent was skipped.");
		return false;
	}

	private void WarnOnce(string kind, string message)
	{
		if (!_warnings.Add(kind))
		{
			return;
		}

		GD.PushWarning($"Statescript: {GetType().Name} {message}");
	}
}
