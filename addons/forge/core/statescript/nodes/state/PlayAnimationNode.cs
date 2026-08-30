// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that owns an animation for as long as it plays, emitting <see cref="OnFinishedPort"/> when it ends.
/// </summary>
/// <remarks>
/// <para>This is the node for an ability whose timing is the animation's timing: a melee swing that hits when the swing
/// lands, a cast bar that ends when the cast animation does. The graph waits on the animator's timing instead of
/// duplicating it in a timer that has to be kept in sync by hand.</para>
/// <para>The node is done when its animation is no longer the one playing, which covers the three ways that happens
/// with one rule: the animation ended, something stopped the player, or another animation took over. A looping
/// animation therefore never finishes on its own and runs until the node is aborted, which is what makes it the
/// channelling shape.</para>
/// <para>Deactivating stops playback under <paramref name="stopOnDeactivate"/>, which reaches an abort, a subgraph
/// ending and the graph stopping alike - an interrupted cast should not leave the caster mid-gesture. A natural finish
/// stops nothing, because by then the animation is over.</para>
/// </remarks>
/// <param name="playerPath">Optional path to the animation player, from the node the entity lives on. Empty means the
/// entity's first animation player child.</param>
/// <param name="animation">The name of the animation to play.</param>
/// <param name="stopOnDeactivate">Whether playback is stopped when the node deactivates before the animation ends.
/// </param>
[StatescriptCategory("Presentation")]
public class PlayAnimationNode(string playerPath = "", string animation = "", bool stopOnDeactivate = true)
	: StateNode<PlayAnimationNodeContext>
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
	/// Output port index for the event emitted when the animation ends.
	/// </summary>
	public const byte OnFinishedPort = 4;

	private readonly PresentationNodeInputs _inputs = new("Play Animation", playerPath);
	private readonly StringName _animation = animation ?? string.Empty;
	private readonly bool _stopOnDeactivate = stopOnDeactivate;

	/// <inheritdoc/>
	public override string Description =>
		"Plays an animation while active, emitting OnFinished when it ends and stopping it on an early exit.";

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
		nodeContext.Player = null;

		if (!_inputs.TryGetAnimationPlayer(
			graphContext,
			InputProperties[EntityInput].BoundName,
			out AnimationPlayer? player))
		{
			return;
		}

		if (_inputs.TryPlayAnimation(
			player,
			_animation,
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[SpeedInput].BoundName),
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[BlendInput].BoundName)))
		{
			nodeContext.Player = player;
		}
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		PlayAnimationNodeContext nodeContext = graphContext.GetNodeContext<PlayAnimationNodeContext>(NodeID);
		AnimationPlayer? player = nodeContext.Player;
		nodeContext.Player = null;

		if (_stopOnDeactivate && player is not null && IsStillPlaying(player))
		{
			player.Stop();
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		PlayAnimationNodeContext nodeContext = graphContext.GetNodeContext<PlayAnimationNodeContext>(NodeID);
		AnimationPlayer? player = nodeContext.Player;

		if (player is null || IsStillPlaying(player))
		{
			return;
		}

		DeactivateNodeAndEmitMessage(graphContext, OnFinishedPort);
	}

	// Godot reports the assigned animation only while the player is playing, so one comparison answers all three ways
	// this node's animation stops being the one running: it ended, it was stopped, or another animation replaced it.
	private bool IsStillPlaying(AnimationPlayer player)
	{
		return GodotObject.IsInstanceValid(player) && player.CurrentAnimation == _animation;
	}
}
