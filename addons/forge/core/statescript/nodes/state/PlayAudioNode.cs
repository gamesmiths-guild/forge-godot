// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that plays a sound for as long as the sound lasts, emitting <see cref="OnFinishedPort"/> once its length
/// has run.
/// </summary>
/// <remarks>
/// <para>The audio counterpart of Play Animation, and the node for sound that belongs to a stretch of gameplay rather
/// than to a moment: a channel hum, a beam loop, a windup whine. The length is the stream's length divided by the
/// pitch, counted in fixed steps from activation. A looping stream, or one whose length Godot cannot tell, never
/// finishes on its own, so the node runs until the ability ends and takes the sound with it.</para>
/// <para>The node is a presentation node: the sound starts in the presentation pass at the end of the frame, so an
/// activation that ends within the frame plays nothing. A sound with nothing to play - no player, or a player with no
/// stream - has no length and finishes on the next step rather than waiting forever, since it is not a sound the graph
/// can be waiting on. The missing player is warned about; a misconfigured presentation node should not be able to
/// stall an ability on top of that.</para>
/// <para>A retrigger while the sound plays is ignored, or with <paramref name="restartOnRetrigger"/> the sound starts
/// over from the beginning, with the volume and pitch resolved again at that moment. Under
/// <paramref name="stopOnDeactivate"/> it replaces the sound rather than layering a second one on a player that allows
/// polyphony.</para>
/// </remarks>
/// <param name="playerPath">Optional path to the audio player, from the node the entity lives on. Empty means the
/// entity's first audio player child.</param>
/// <param name="stopOnDeactivate">Whether the sound is stopped when the node deactivates before it ends.</param>
/// <param name="restartOnRetrigger">Whether a retrigger replays the sound instead of being ignored.</param>
[StatescriptCategory("Presentation")]
public class PlayAudioNode(string playerPath = "", bool stopOnDeactivate = true, bool restartOnRetrigger = false)
	: StateNode<PlayAudioNodeContext>(restartOnRetrigger)
{
	/// <summary>
	/// Input property index for the entity that owns the player. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the optional volume, in decibels.
	/// </summary>
	public const byte VolumeDbInput = 1;

	/// <summary>
	/// Input property index for the optional pitch scale.
	/// </summary>
	public const byte PitchInput = 2;

	/// <summary>
	/// Output port index for the event emitted when the sound's length has run.
	/// </summary>
	public const byte OnFinishedPort = 4;

	private readonly PresentationNodeInputs _inputs = new("Play Audio", playerPath);
	private readonly bool _stopOnDeactivate = stopOnDeactivate;

	/// <inheritdoc/>
	public override string Description =>
		"Plays a sound while active, emitting OnFinished once its length has run and optionally stopping it on an " +
		"early exit.";

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
		inputProperties.Add(new InputProperty("Volume Db", typeof(double), IsOptional: true));
		inputProperties.Add(new InputProperty("Pitch", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		PlayAudioNodeContext nodeContext = graphContext.GetNodeContext<PlayAudioNodeContext>(NodeID);
		nodeContext.ElapsedTime = 0;
		nodeContext.Duration = 0;
		nodeContext.Player = null;

		// Resolved here, once, for the presentation pass to play: see PlayAudioNodeContext.
		if (!_inputs.TryGetAudioPlayer(graphContext, InputProperties[EntityInput].BoundName, out Node? player))
		{
			return;
		}

		nodeContext.Player = player;
		nodeContext.VolumeDb =
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[VolumeDbInput].BoundName);
		nodeContext.Pitch = PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[PitchInput].BoundName);
		nodeContext.Duration = AudioPlayers.PlayLength(player, nodeContext.Pitch);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		// The presentation pass stops the sound.
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
		PlayAudioNodeContext nodeContext = graphContext.GetNodeContext<PlayAudioNodeContext>(NodeID);
		nodeContext.ElapsedTime += deltaTime;

		if (nodeContext.ElapsedTime >= nodeContext.Duration)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFinishedPort);
		}
	}

	/// <inheritdoc/>
	protected override object? OnPresentationActivated(GraphContext graphContext)
	{
		PlayAudioNodeContext nodeContext = graphContext.GetNodeContext<PlayAudioNodeContext>(NodeID);
		Node? player = nodeContext.Player;

		if (player is null || !GodotObject.IsInstanceValid(player))
		{
			return null;
		}

		AudioPlayers.TryPlay(player, nodeContext.VolumeDb, nodeContext.Pitch);
		return player;
	}

	/// <inheritdoc/>
	protected override void OnPresentationDeactivated(object? presentation)
	{
		if (_stopOnDeactivate
			&& presentation is Node player
			&& GodotObject.IsInstanceValid(player)
			&& AudioPlayers.IsPlaying(player))
		{
			AudioPlayers.Stop(player);
		}
	}
}
