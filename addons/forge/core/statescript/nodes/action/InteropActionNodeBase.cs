// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Base for the action nodes that reach into a scene node directly.
/// </summary>
/// <remarks>
/// <para>Declares the node input every one of them takes and resolves it, falling back to the node the ability's owner
/// lives on so "act on me" is what an unbound row means.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
public abstract class InteropActionNodeBase : ActionNode
{
	/// <summary>
	/// Input property index for the node to act on. Unbound means the node the ability's owner lives on.
	/// </summary>
	public const byte NodeInput = 0;

	private readonly HashSet<string> _warnings = [];

	/// <summary>
	/// Adds this node's own input properties and output variables. The node input is already declared.
	/// </summary>
	/// <param name="inputProperties">The input property list to add to.</param>
	/// <param name="outputVariables">The output variable list to add to.</param>
	protected abstract void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables);

	/// <summary>
	/// Performs this node's work against the resolved scene node.
	/// </summary>
	/// <param name="node">The node to act on.</param>
	/// <param name="graphContext">The graph execution context, for resolving this node's own inputs.</param>
	protected abstract void ExecuteOn(Node node, GraphContext graphContext);

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Node", typeof(Node), IsOptional: true));
		DefineInteropParameters(inputProperties, outputVariables);
	}

	/// <inheritdoc/>
	protected sealed override void Execute(GraphContext graphContext)
	{
		Node? node = InteropNodeInputs.ResolveNodeOrOwner(graphContext, InputProperties[NodeInput].BoundName);

		if (node is null)
		{
			WarnOnce("resolved no node to act on, and the ability's owner has none either.");
			return;
		}

		ExecuteOn(node, graphContext);
	}

	/// <summary>
	/// Reports a misconfiguration once, naming this node.
	/// </summary>
	/// <remarks>
	/// The interop nodes fail in several distinct ways - a node that is not there, a property or method the scene does
	/// not declare, a value that will not convert - and each of them means the write silently did nothing. Suppression
	/// is per message rather than per node, so a second, different problem is not hidden by the first having already
	/// been reported.
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
