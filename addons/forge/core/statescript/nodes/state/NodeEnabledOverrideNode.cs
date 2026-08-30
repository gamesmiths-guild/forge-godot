// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that turns one of a scene node's switches on or off while active and puts it back when it ends.
/// </summary>
/// <remarks>
/// <para>Every deactivation path restores the original setting, including an abort. That is the whole reason this node
/// exists rather than a pair of writes the graph has to remember on each exit: a stealth ability cancelled mid-cast
/// must not leave the character permanently invisible, and a hitbox switched off for a parry window must come back.
/// </para>
/// <para>The setting found at activation is what gets restored, so a node the scene authored as already hidden stays
/// hidden, and two overlapping overrides of the same switch resolve to whichever ends last rather than to a value
/// neither of them intended.</para>
/// </remarks>
/// <param name="aspect">Which switch to change.</param>
[StatescriptCategory("Interop")]
public class NodeEnabledOverrideNode(NodeEnabledAspect aspect = NodeEnabledAspect.Visible)
	: InteropStateNodeBase<NodeEnabledOverrideNodeContext>
{
	/// <summary>
	/// Input property index for the setting to hold while active.
	/// </summary>
	public const byte EnabledInput = 1;

	private readonly NodeEnabledAspect _aspect = aspect;

	/// <inheritdoc/>
	public override string Description =>
		"Holds a scene node's visibility, processing or monitoring while active, restoring it on deactivation or "
		+ "abort.";

	/// <inheritdoc/>
	protected override void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Enabled", typeof(bool)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		NodeEnabledOverrideNodeContext nodeContext =
			graphContext.GetNodeContext<NodeEnabledOverrideNodeContext>(NodeID);
		nodeContext.Node = null;

		Node? node = ResolveNode(graphContext);

		if (node is null)
		{
			WarnOnce("resolved no node to act on, and the ability's owner has none either.");
			return;
		}

		if (!NodeEnabledAspects.TryRead(node, _aspect, out bool original))
		{
			WarnOnce(NodeEnabledAspects.DescribeUnsupported(node, _aspect));
			return;
		}

		graphContext.TryResolve(InputProperties[EnabledInput].BoundName, out bool enabled);

		nodeContext.Node = node;
		nodeContext.OriginalEnabled = original;

		NodeEnabledAspects.TryWrite(node, _aspect, enabled);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		NodeEnabledOverrideNodeContext nodeContext =
			graphContext.GetNodeContext<NodeEnabledOverrideNodeContext>(NodeID);
		Node? node = nodeContext.Node;
		nodeContext.Node = null;

		if (node is not null && GodotObject.IsInstanceValid(node))
		{
			NodeEnabledAspects.TryWrite(node, _aspect, nodeContext.OriginalEnabled);
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		// Nothing to do per tick: the override is applied once on activation and undone once on deactivation.
	}
}
