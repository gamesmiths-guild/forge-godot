// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that plays an animation for as long as the animation lasts, emitting <see cref="OnFinishedPort"/> once
/// its length has run.
/// </summary>
/// <remarks>
/// <para>This is the node for an ability whose timing is the animation's timing: a melee swing that hits when the swing
/// lands, a cast bar that ends when the cast animation does. The graph waits on the animation's length instead of
/// duplicating it in a timer that has to be kept in sync by hand.</para>
/// <para>The length is the clip's length divided by the speed it plays at, counted in fixed steps from activation, so
/// the node finishes on the same step on every machine, whatever the engine is doing with playback. Another animation
/// taking over the player does not end it. A looping clip, or a speed of zero, never finishes on its own and runs until
/// the node is aborted, which is what makes it the channelling shape.</para>
/// <para>The node is a presentation node: the animation starts in the presentation pass at the end of the frame, so an
/// activation that ends within the frame plays nothing. A clip the player does not have, or no player at all, counts as
/// one with no length, so the node warns and finishes on the next step rather than holding open a graph that is waiting
/// on it. A misconfigured presentation node should not be able to stall an ability.</para>
/// <para>Once the node ends early, the presentation pass stops playback under <paramref name="stopOnDeactivate"/>,
/// which reaches an abort, a subgraph ending and the graph stopping alike - an interrupted cast should not leave the
/// caster mid-gesture. A natural finish stops nothing, because by then the animation is over.</para>
/// <para>A retrigger while the animation plays is ignored, or with <paramref name="restartOnRetrigger"/> the animation
/// plays again from its start, with the speed and blend resolved again and its length counted from the restart.</para>
/// </remarks>
/// <param name="playerPath">Optional path to the animation player, from the node the entity lives on. Empty means the
/// entity's first animation player child.</param>
/// <param name="animation">The name of the animation to play.</param>
/// <param name="stopOnDeactivate">Whether playback is stopped when the node deactivates before the animation ends.
/// </param>
/// <param name="restartOnRetrigger">Whether a retrigger replays the animation instead of being ignored.</param>
[StatescriptCategory("Presentation")]
public class PlayAnimationNode(
	string playerPath = "",
	string animation = "",
	bool stopOnDeactivate = true,
	bool restartOnRetrigger = false) : StateNode<PlayAnimationNodeContext>(restartOnRetrigger)
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

	/// <summary>
	/// Output port index for the event emitted when the animation's length has run.
	/// </summary>
	public const byte OnFinishedPort = 4;

	private readonly PresentationNodeInputs _inputs = new("Play Animation", playerPath);
	private readonly StringName _animation = animation ?? string.Empty;
	private readonly bool _stopOnDeactivate = stopOnDeactivate;

	/// <inheritdoc/>
	public override string Description =>
		"Plays an animation while active, emitting OnFinished once its length has run and optionally stopping it on " +
		"an early exit.";

	/// <inheritdoc/>
	public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnFinishedPort, "OnFinished"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Speed", typeof(double), IsOptional: true));
		inputProperties.Add(new InputProperty("Blend", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		PlayAnimationNodeContext nodeContext = graphContext.GetNodeContext<PlayAnimationNodeContext>(NodeID);
		nodeContext.ElapsedTime = 0;
		nodeContext.Duration = 0;
		nodeContext.Player = null;

		// Resolved here, once, for the presentation pass to play: see PlayAnimationNodeContext.
		if (!_inputs.TryGetAnimationPlayer(
			graphContext,
			InputProperties[EntityInput].BoundName,
			out AnimationPlayer? player))
		{
			return;
		}

		nodeContext.Player = player;
		nodeContext.Speed = PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[SpeedInput].BoundName);
		nodeContext.Blend = PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[BlendInput].BoundName);
		nodeContext.Duration = PlayLength(player, nodeContext.Speed);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		// The presentation pass stops playback.
	}

	/// <inheritdoc/>
	protected override void OnRestart(GraphContext graphContext)
	{
		// The presentation pass plays the restart from the start; only the length starts over here.
		OnActivate(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		PlayAnimationNodeContext nodeContext = graphContext.GetNodeContext<PlayAnimationNodeContext>(NodeID);
		nodeContext.ElapsedTime += deltaTime;

		if (nodeContext.ElapsedTime >= nodeContext.Duration)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFinishedPort);
		}
	}

	/// <inheritdoc/>
	protected override object? OnPresentationActivated(GraphContext graphContext)
	{
		PlayAnimationNodeContext nodeContext = graphContext.GetNodeContext<PlayAnimationNodeContext>(NodeID);
		AnimationPlayer? player = nodeContext.Player;

		if (player is null || !GodotObject.IsInstanceValid(player))
		{
			return null;
		}

		// Godot's Play leaves an animation that is already playing where it is, and the length is counted from the
		// start, so it is stopped to play from there.
		if (IsStillPlaying(player))
		{
			player.Stop();

			// Stopping emits current_animation_changed, and a listener can end this node: playing now would outlive it
			// until the next pass.
			if (!nodeContext.Active)
			{
				return null;
			}
		}

		return _inputs.TryPlayAnimation(player, _animation, nodeContext.Speed, nodeContext.Blend) ? player : null;
	}

	/// <inheritdoc/>
	protected override void OnPresentationDeactivated(object? presentation)
	{
		if (_stopOnDeactivate && presentation is AnimationPlayer player && IsStillPlaying(player))
		{
			player.Stop();
		}
	}

	// Godot reports the assigned animation only while the player is playing, so one comparison tells whether this
	// node's animation is still the one running.
	private bool IsStillPlaying(AnimationPlayer player)
	{
		return GodotObject.IsInstanceValid(player) && player.CurrentAnimation == _animation;
	}

	private double PlayLength(AnimationPlayer player, double? speed)
	{
		if (!player.HasAnimation(_animation))
		{
			return 0;
		}

		Animation clip = player.GetAnimation(_animation);
		double rate = Math.Abs((speed ?? 1) * player.SpeedScale);

		return clip.LoopMode != Animation.LoopModeEnum.None || rate <= 0
			? double.PositiveInfinity
			: clip.Length / rate;
	}
}
