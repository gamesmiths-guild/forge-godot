// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that starts an animation on an entity's animation player and forgets about it.
/// </summary>
/// <remarks>
/// <para>Fire and forget: the graph moves on immediately and nothing here follows the animation to its end. Use it for
/// something that plays alongside whatever the ability does next - a flinch, a cast flourish, an idle swap. When the
/// ability's timing <em>is</em> the animation's timing, Play Animation owns playback and reports when it finishes.
/// </para>
/// <para>Animation-keyed timing needs no node at all: an <see cref="AnimationPlayer"/> method track raising a Forge
/// event is picked up by the core Event Listener node, which is how a hit window lands on the exact frame the animator
/// chose.</para>
/// </remarks>
/// <param name="playerPath">Optional path to the animation player, from the node the entity lives on. Empty means the
/// entity's first animation player child.</param>
/// <param name="animation">The name of the animation to play.</param>
[StatescriptCategory("Presentation")]
public class PlayAnimationOneShotNode(string playerPath = "", string animation = "") : ActionNode
{
	/// <summary>
	/// Input property index for the entity to animate. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the optional playback speed multiplier.
	/// </summary>
	public const byte SpeedInput = 1;

	/// <summary>
	/// Input property index for the optional blend time, in seconds.
	/// </summary>
	public const byte BlendInput = 2;

	private readonly PresentationNodeInputs _inputs = new("Play Animation One Shot", playerPath);
	private readonly StringName _animation = animation ?? string.Empty;

	/// <inheritdoc/>
	public override string Description => "Starts an animation on an entity's animation player and moves on.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Speed", typeof(double), IsOptional: true));
		inputProperties.Add(new InputProperty("Blend", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!_inputs.TryGetAnimationPlayer(
			graphContext,
			InputProperties[EntityInput].BoundName,
			out AnimationPlayer? player))
		{
			return;
		}

		_inputs.TryPlayAnimation(
			player,
			_animation,
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[SpeedInput].BoundName),
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[BlendInput].BoundName));
	}
}
