// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// <para>Visibility only changes what is seen, so for <see cref="NodeEnabledAspect.Visible"/> the node is a
/// presentation node: the presentation pass hides or shows at the end of the frame the node activates in and restores
/// at the end of the frame it ends in, and an activation that ends within the frame changes nothing. Processing and
/// monitoring change gameplay, so for those the node is a simulation node and switches the moment it activates and
/// ends.</para>
/// <para>The setting found when the switch happens is what gets restored, so a node the scene authored as already
/// hidden stays hidden, and two overlapping overrides of the same switch resolve to whichever ends last rather than to
/// a value neither of them intended.</para>
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
	public override NodeNetworkKind NetworkKind =>
		_aspect == NodeEnabledAspect.Visible ? NodeNetworkKind.Presentation : NodeNetworkKind.Simulation;

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

		// A presentation switch waits for the presentation pass.
		if (NetworkKind == NodeNetworkKind.Simulation && TryHold(graphContext, out Node? node, out bool original))
		{
			nodeContext.Node = node;
			nodeContext.OriginalEnabled = original;
		}
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		NodeEnabledOverrideNodeContext nodeContext =
			graphContext.GetNodeContext<NodeEnabledOverrideNodeContext>(NodeID);
		Node? node = nodeContext.Node;
		nodeContext.Node = null;

		Restore(node, nodeContext.OriginalEnabled);
	}

	/// <inheritdoc/>
	protected override object? OnPresentationActivated(GraphContext graphContext)
	{
		return TryHold(graphContext, out Node? node, out bool original) ? new HeldSwitch(node, original) : null;
	}

	/// <inheritdoc/>
	protected override void OnPresentationDeactivated(object? presentation)
	{
		if (presentation is HeldSwitch held)
		{
			Restore(held.Node, held.OriginalEnabled);
		}
	}

	private bool TryHold(GraphContext graphContext, [NotNullWhen(true)] out Node? node, out bool original)
	{
		original = false;
		node = ResolveNode(graphContext);

		if (node is null)
		{
			WarnOnce("resolved no node to act on, and the ability's owner has none either.");
			return false;
		}

		if (!NodeEnabledAspects.TryRead(node, _aspect, out original))
		{
			WarnOnce(NodeEnabledAspects.DescribeUnsupported(node, _aspect));
			node = null;
			return false;
		}

		if (!graphContext.TryResolve(InputProperties[EnabledInput].BoundName, out bool enabled))
		{
			// Nothing is captured either: an override that could not read what to hold must leave the node alone, not
			// hold it at the false a failed read returns and then claim to restore it.
			WarnOnce("could not resolve whether to enable or disable. The override was skipped.");
			node = null;
			return false;
		}

		NodeEnabledAspects.TryWrite(node, _aspect, enabled);
		return true;
	}

	private void Restore(Node? node, bool original)
	{
		if (node is not null && GodotObject.IsInstanceValid(node))
		{
			NodeEnabledAspects.TryWrite(node, _aspect, original);
		}
	}

	private sealed record HeldSwitch(Node Node, bool OriginalEnabled);
}
