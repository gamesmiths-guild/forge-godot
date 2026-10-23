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
	// The player's own volume and pitch, kept on it the first time one is overridden. Unbound then means the player's
	// own setting rather than whatever override played on it last: an override is presentation, which a peer that does
	// not present never writes, so a length read from it would differ between peers.
	private static readonly StringName _volumeDb = "volume_db";
	private static readonly StringName _pitchScale = "pitch_scale";
	private static readonly StringName _authoredVolumeDb = "forge_authored_volume_db";
	private static readonly StringName _authoredPitchScale = "forge_authored_pitch_scale";

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
	/// Gets how long a player's stream plays at a pitch, in seconds.
	/// </summary>
	/// <param name="player">The audio player.</param>
	/// <param name="pitch">The pitch scale it plays at, or <see langword="null"/> for the player's own.</param>
	/// <returns>The stream's length divided by the pitch; infinite for a looping stream, one whose length is unknown or
	/// a pitch of zero; zero when the player has no stream.</returns>
	public static double PlayLength(Node player, double? pitch)
	{
		AudioStream? stream = player switch
		{
			AudioStreamPlayer plain => plain.Stream,
			AudioStreamPlayer2D player2D => player2D.Stream,
			AudioStreamPlayer3D player3D => player3D.Stream,
			_ => null,
		};

		if (stream is null)
		{
			return 0;
		}

		double length = stream.GetLength();
		double rate = pitch ?? Authored(player, _pitchScale, _authoredPitchScale);

		return Loops(stream) || length <= 0 || rate <= 0 ? double.PositiveInfinity : length / rate;
	}

	/// <summary>
	/// Plays a player from the start, overriding its authored volume and pitch when values are given.
	/// </summary>
	/// <param name="player">The audio player to play.</param>
	/// <param name="volumeDb">The volume to set, or <see langword="null"/> for the player's own.</param>
	/// <param name="pitch">The pitch scale to set, or <see langword="null"/> for the player's own.</param>
	/// <returns><see langword="true"/> if the node was an audio player and was played.</returns>
	public static bool TryPlay(Node player, double? volumeDb, double? pitch)
	{
		if (!IsPlayer(player))
		{
			return false;
		}

		Override(player, _volumeDb, _authoredVolumeDb, volumeDb);
		Override(player, _pitchScale, _authoredPitchScale, pitch);

		switch (player)
		{
			case AudioStreamPlayer plain:
				plain.Play();
				return true;

			case AudioStreamPlayer2D player2D:
				player2D.Play();
				return true;

			case AudioStreamPlayer3D player3D:
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

	private static double Authored(Node player, StringName property, StringName authored)
	{
		return (player.HasMeta(authored) ? player.GetMeta(authored) : player.Get(property)).AsDouble();
	}

	// Sets the value, or puts the player's own back when there is none, keeping the player's own the first time it is
	// overridden.
	private static void Override(Node player, StringName property, StringName authored, double? value)
	{
		if (value is null)
		{
			if (player.HasMeta(authored))
			{
				player.Set(property, player.GetMeta(authored));
			}

			return;
		}

		if (!player.HasMeta(authored))
		{
			player.SetMeta(authored, player.Get(property));
		}

		player.Set(property, (float)value.Value);
	}

	private static bool Loops(AudioStream stream)
	{
		return stream switch
		{
			AudioStreamWav wav => wav.LoopMode != AudioStreamWav.LoopModeEnum.Disabled,
			AudioStreamOggVorbis ogg => ogg.Loop,
			AudioStreamMP3 mp3 => mp3.Loop,
			_ => false,
		};
	}
}
