// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// The three Godot audio players under one set of operations.
/// </summary>
/// <remarks>
/// They share no base beyond <see cref="Node"/> - each positional one derives from its dimension's spatial node - so
/// anything that plays "the entity's audio player" without caring which of the three it is has to switch over all
/// three. This is that switch, written once for the audio nodes and the audio cue handler.
/// </remarks>
internal static class AudioPlayers
{
	/// <summary>
	/// Gets whether a node is one of the audio players.
	/// </summary>
	/// <param name="node">The node to test.</param>
	/// <returns><see langword="true"/> if the node is an audio player.</returns>
	public static bool IsPlayer(Node node)
	{
		return node is AudioStreamPlayer or AudioStreamPlayer2D or AudioStreamPlayer3D;
	}

	/// <summary>
	/// Plays a player from the start, overriding its authored volume and pitch when values are given.
	/// </summary>
	/// <param name="player">The audio player to play.</param>
	/// <param name="volumeDb">The volume to set, or <see langword="null"/> to keep the authored one.</param>
	/// <param name="pitch">The pitch scale to set, or <see langword="null"/> to keep the authored one.</param>
	/// <returns><see langword="true"/> if the node was an audio player and was played.</returns>
	public static bool TryPlay(Node player, double? volumeDb, double? pitch)
	{
		switch (player)
		{
			case AudioStreamPlayer plain:
				plain.VolumeDb = (float)(volumeDb ?? plain.VolumeDb);
				plain.PitchScale = (float)(pitch ?? plain.PitchScale);
				plain.Play();
				return true;

			case AudioStreamPlayer2D player2D:
				player2D.VolumeDb = (float)(volumeDb ?? player2D.VolumeDb);
				player2D.PitchScale = (float)(pitch ?? player2D.PitchScale);
				player2D.Play();
				return true;

			case AudioStreamPlayer3D player3D:
				player3D.VolumeDb = (float)(volumeDb ?? player3D.VolumeDb);
				player3D.PitchScale = (float)(pitch ?? player3D.PitchScale);
				player3D.Play();
				return true;

			default:
				return false;
		}
	}

	/// <summary>
	/// Gets whether a player is currently making sound.
	/// </summary>
	/// <param name="player">The audio player to check.</param>
	/// <returns><see langword="true"/> if the node is an audio player and is playing.</returns>
	public static bool IsPlaying(Node player)
	{
		return player switch
		{
			AudioStreamPlayer plain => plain.Playing,
			AudioStreamPlayer2D player2D => player2D.Playing,
			AudioStreamPlayer3D player3D => player3D.Playing,
			_ => false,
		};
	}

	/// <summary>
	/// Stops a player.
	/// </summary>
	/// <param name="player">The audio player to stop.</param>
	public static void Stop(Node player)
	{
		switch (player)
		{
			case AudioStreamPlayer plain:
				plain.Stop();
				break;

			case AudioStreamPlayer2D player2D:
				player2D.Stop();
				break;

			case AudioStreamPlayer3D player3D:
				player3D.Stop();
				break;
		}
	}
}
