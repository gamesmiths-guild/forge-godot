// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// Base for the state nodes that hold something open on a scene node.
/// </summary>
/// <remarks>
/// The state-side counterpart of <c>InteropActionNodeBase</c>: same node input, same owner fallback, same once-per
/// -message reporting. Two of these are overrides that put a value back when they end, and one watches a signal.
/// </remarks>
/// <typeparam name="TContext">The node's runtime context type.</typeparam>
public abstract class InteropStateNodeBase<TContext> : StateNode<TContext>
	where TContext : StateNodeContext, new()
{
	/// <summary>
	/// Input property index for the node to act on. Unbound means the node the ability's owner lives on.
	/// </summary>
#pragma warning disable RCS1158 // Static member in generic type should use a type parameter
	public const byte NodeInput = 0;
#pragma warning restore RCS1158 // Static member in generic type should use a type parameter

	private readonly HashSet<string> _warnings = [];

	/// <summary>
	/// Adds this node's own input properties and output variables. The node input is already declared.
	/// </summary>
	/// <param name="inputProperties">The input property list to add to.</param>
	/// <param name="outputVariables">The output variable list to add to.</param>
	protected abstract void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables);

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Node", typeof(Node), IsOptional: true));
		DefineInteropParameters(inputProperties, outputVariables);
	}

	/// <summary>
	/// Resolves the node this activation acts on, falling back to the node the ability's owner lives on.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <returns>The resolved node, or <see langword="null"/> when neither is available.</returns>
	protected Node? ResolveNode(GraphContext graphContext)
	{
		return InteropNodeInputs.ResolveNodeOrOwner(graphContext, InputProperties[NodeInput].BoundName);
	}

	/// <summary>
	/// Reports a misconfiguration once, naming this node.
	/// </summary>
	/// <remarks>
	/// Suppression is per message rather than per node, so a second, different problem is not hidden by the first
	/// having already been reported.
	/// </remarks>
	/// <param name="message">What is wrong, completing "Statescript: {node type} ".</param>
	protected void WarnOnce(string message)
	{
		if (!_warnings.Add(message))
		{
			return;
		}

		GD.PushWarning($"Statescript: {GetType().Name} {message}");
	}
}
