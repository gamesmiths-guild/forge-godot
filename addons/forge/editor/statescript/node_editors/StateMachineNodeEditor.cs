// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Statescript.Nodes.State;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for the <c>StateMachineNode</c>. The state count is a constructor argument, so this editor renders it as
/// a spin box — or, when an enum is bound, follows the enum and names each state subgraph port after its member, which
/// is what makes a state machine readable at a glance.
/// </summary>
[Tool]
internal sealed partial class StateMachineNodeEditor : PortCountNodeEditorBase
{
	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.State.StateMachineNode";

	/// <inheritdoc/>
	protected override string CountConfigKey => "stateCount";

	/// <inheritdoc/>
	protected override string CountLabel => "States";

	/// <inheritdoc/>
	protected override int DefaultCount => 2;

	/// <inheritdoc/>
	protected override int MinCount => 1;

	/// <inheritdoc/>
	protected override int MaxCount => byte.MaxValue - StateMachineNode.FirstStatePort + 1;

	/// <inheritdoc/>
	protected override int FirstEnumPortIndex => StateMachineNode.FirstStatePort;

	/// <inheritdoc/>
	protected override int GetEnumPortCount(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		// Everything from the first state subgraph port on is a state; the ports before it are the node's lifecycle and
		// OnStateChanged ports.
		return typeInfo.OutputPortLabels.Length - StateMachineNode.FirstStatePort;
	}
}
#endif
