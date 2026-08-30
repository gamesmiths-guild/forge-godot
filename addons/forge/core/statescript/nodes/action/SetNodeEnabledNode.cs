// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that turns one of a scene node's switches on or off.
/// </summary>
/// <remarks>
/// These are the switches Set Node Property cannot reach with a path alone: two of them are named differently in 2D and
/// 3D, and processing is a method rather than a property. Hiding a decoy, arming a trap by turning its area's
/// monitoring on, or stopping a spawner's own script from running are all one row here. The write is permanent, for
/// state that outlives the ability; Node Enabled Override is the form that puts the switch back.
/// </remarks>
/// <param name="aspect">Which switch to write.</param>
[StatescriptCategory("Interop")]
public sealed class SetNodeEnabledNode(NodeEnabledAspect aspect = NodeEnabledAspect.Visible) : InteropActionNodeBase
{
	/// <summary>
	/// Input property index for the value to write.
	/// </summary>
	public const byte EnabledInput = 1;

	private readonly NodeEnabledAspect _aspect = aspect;

	/// <inheritdoc/>
	public override string Description => "Turns a scene node's visibility, processing or monitoring on or off.";

	/// <inheritdoc/>
	protected override void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Enabled", typeof(bool)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node node, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[EnabledInput].BoundName, out bool enabled))
		{
			WarnOnce("could not resolve whether to enable or disable. The write was skipped.");
			return;
		}

		if (!NodeEnabledAspects.TryWrite(node, _aspect, enabled))
		{
			WarnOnce(NodeEnabledAspects.DescribeUnsupported(node, _aspect));
		}
	}
}
