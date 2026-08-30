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
/// State node that holds a sound for as long as it plays, emitting <see cref="OnFinishedPort"/> when it ends.
/// </summary>
/// <remarks>
/// <para>The audio counterpart of Play Animation, and the node for sound that belongs to a stretch of gameplay rather
/// than to a moment: a channel hum, a beam loop, a windup whine. A looping stream never finishes on its own, so the
/// node runs until the ability ends and takes the sound with it.</para>
/// <para>A sound that never started - no player, or a player with no stream - reports finished on its first update
/// rather than waiting forever, since it is not a sound the graph can be waiting on. The missing player is warned
/// about; a misconfigured presentation node should not be able to stall an ability on top of that.</para>
/// </remarks>
/// <param name="playerPath">Optional path to the audio player, from the node the entity lives on. Empty means the
/// entity's first audio player child.</param>
/// <param name="stopOnDeactivate">Whether the sound is stopped when the node deactivates before it ends.</param>
[StatescriptCategory("Presentation")]
public class PlayAudioNode(string playerPath = "", bool stopOnDeactivate = true) : StateNode<PlayAudioNodeContext>
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
	/// Output port index for the event emitted when the sound ends.
	/// </summary>
	public const byte OnFinishedPort = 4;

	private readonly PresentationNodeInputs _inputs = new("Play Audio", playerPath);
	private readonly bool _stopOnDeactivate = stopOnDeactivate;

	/// <inheritdoc/>
	public override string Description =>
		"Plays a sound while active, emitting OnFinished when it ends and optionally stopping it on an early exit.";

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
		nodeContext.Player = null;

		if (!_inputs.TryGetAudioPlayer(graphContext, InputProperties[EntityInput].BoundName, out Node? player))
		{
			return;
		}

		AudioPlayers.TryPlay(
			player,
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[VolumeDbInput].BoundName),
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[PitchInput].BoundName));

		nodeContext.Player = player;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		PlayAudioNodeContext nodeContext = graphContext.GetNodeContext<PlayAudioNodeContext>(NodeID);
		Node? player = nodeContext.Player;
		nodeContext.Player = null;

		if (_stopOnDeactivate && player is not null && GodotObject.IsInstanceValid(player))
		{
			AudioPlayers.Stop(player);
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		PlayAudioNodeContext nodeContext = graphContext.GetNodeContext<PlayAudioNodeContext>(NodeID);
		Node? player = nodeContext.Player;

		if (player is not null && GodotObject.IsInstanceValid(player) && AudioPlayers.IsPlaying(player))
		{
			return;
		}

		DeactivateNodeAndEmitMessage(graphContext, OnFinishedPort);
	}
}
