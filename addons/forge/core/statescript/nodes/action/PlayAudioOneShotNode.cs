// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that plays an entity's audio player once and forgets about it.
/// </summary>
/// <remarks>
/// <para>Plays a player that already exists in the entity's scene rather than creating one, so the stream, the bus and
/// the 3D attenuation stay authored where a sound designer can see them. Any of the three player types will do: the
/// node does not care whether the sound is positional, and a scene that is 2D or 3D simply carries that dimension's
/// player.</para>
/// <para>Volume and pitch are inputs rather than settings so a graph can vary them - a pitch scaled by a combo counter,
/// a volume by charge. Left unbound, the player's own authored values stand.</para>
/// </remarks>
/// <param name="playerPath">Optional path to the audio player, from the node the entity lives on. Empty means the
/// entity's first audio player child.</param>
[StatescriptCategory("Presentation")]
public class PlayAudioOneShotNode(string playerPath = "") : ActionNode
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

	private readonly PresentationNodeInputs _inputs = new("Play Audio One Shot", playerPath);

	/// <inheritdoc/>
	public override string Description => "Plays an entity's audio player once and moves on.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Volume Db", typeof(double), IsOptional: true));
		inputProperties.Add(new InputProperty("Pitch", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!_inputs.TryGetAudioPlayer(graphContext, InputProperties[EntityInput].BoundName, out Node? player))
		{
			return;
		}

		AudioPlayers.TryPlay(
			player,
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[VolumeDbInput].BoundName),
			PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[PitchInput].BoundName));
	}
}
