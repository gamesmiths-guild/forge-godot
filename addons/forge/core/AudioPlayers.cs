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
		AudioStream? stream = StreamOf(player);

		if (stream is null)
		{
			return 0;
		}

		double length = stream.GetLength();
		double rate = pitch ?? Authored(player, _pitchScale, _authoredPitchScale);

		return Loops(stream) || length <= 0 || rate <= 0 ? double.PositiveInfinity : length / rate;
	}

	/// <summary>
	/// Gets where in a player's stream a sound played for some time at a pitch has got to.
	/// </summary>
	/// <param name="player">The audio player.</param>
	/// <param name="pitch">The pitch scale it plays at, or <see langword="null"/> for the player's own.</param>
	/// <param name="elapsedTime">How long the sound has played, in seconds.</param>
	/// <returns>The position in the stream, in seconds, wrapped around for a looping stream.</returns>
	public static double StreamPosition(Node player, double? pitch, double elapsedTime)
	{
		AudioStream? stream = StreamOf(player);
		double position = elapsedTime * (pitch ?? Authored(player, _pitchScale, _authoredPitchScale));
		double length = stream?.GetLength() ?? 0;

		return stream is not null && Loops(stream) && length > 0 ? LoopedPosition(stream, position, length) : position;
	}

	/// <summary>
	/// Plays a player, overriding its authored volume and pitch when values are given.
	/// </summary>
	/// <remarks>
	/// A sound started on a player that can't process, as while the tree is paused, waits for it to process again, the
	/// way the sounds that were playing when the tree paused do.
	/// </remarks>
	/// <param name="player">The audio player to play.</param>
	/// <param name="volumeDb">The volume to set, or <see langword="null"/> for the player's own.</param>
	/// <param name="pitch">The pitch scale to set, or <see langword="null"/> for the player's own.</param>
	/// <param name="fromPosition">Where in the stream to start, in seconds.</param>
	/// <returns><see langword="true"/> if the node was an audio player and was played.</returns>
	public static bool TryPlay(Node player, double? volumeDb, double? pitch, double fromPosition = 0)
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
				plain.Play((float)fromPosition);
				break;

			case AudioStreamPlayer2D player2D:
				player2D.Play((float)fromPosition);
				break;

			case AudioStreamPlayer3D player3D:
				player3D.Play((float)fromPosition);
				break;
		}

		HoldWhilePaused(player);
		return true;
	}

	/// <summary>
	/// Moves a playing player's sound to a position in its stream.
	/// </summary>
	/// <param name="player">The audio player.</param>
	/// <param name="position">The position, in seconds.</param>
	public static void Seek(Node player, double position)
	{
		switch (player)
		{
			case AudioStreamPlayer plain:
				plain.Seek((float)position);
				break;

			case AudioStreamPlayer2D player2D:
				player2D.Seek((float)position);
				break;

			case AudioStreamPlayer3D player3D:
				player3D.Seek((float)position);
				break;
		}

		HoldWhilePaused(player);
	}

	/// <summary>
	/// Gets where in its stream a player's sound is playing.
	/// </summary>
	/// <param name="player">The audio player.</param>
	/// <returns>The position, in seconds.</returns>
	public static double PlaybackPosition(Node player)
	{
		return player switch
		{
			AudioStreamPlayer plain => plain.GetPlaybackPosition(),
			AudioStreamPlayer2D player2D => player2D.GetPlaybackPosition(),
			AudioStreamPlayer3D player3D => player3D.GetPlaybackPosition(),
			_ => 0,
		};
	}

	/// <summary>
	/// Gets whether a player has a sound going, playing or held while it can't process.
	/// </summary>
	/// <remarks>
	/// Godot counts a held sound as not playing, though it plays on once the player processes again.
	/// </remarks>
	/// <param name="player">The audio player to check.</param>
	/// <returns><see langword="true"/> if the node is an audio player with a sound going.</returns>
	public static bool IsPlaying(Node player)
	{
		return player switch
		{
			AudioStreamPlayer plain => plain.Playing || plain.StreamPaused,
			AudioStreamPlayer2D player2D => player2D.Playing || player2D.StreamPaused,
			AudioStreamPlayer3D player3D => player3D.Playing || player3D.StreamPaused,
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

	private static AudioStream? StreamOf(Node player)
	{
		return player switch
		{
			AudioStreamPlayer plain => plain.Stream,
			AudioStreamPlayer2D player2D => player2D.Stream,
			AudioStreamPlayer3D player3D => player3D.Stream,
			_ => null,
		};
	}

	// Godot starts a sound even on a player that can't process, and resumes the paused ones once it can again, so a
	// sound started or moved then is paused at once to wait with them.
	private static void HoldWhilePaused(Node player)
	{
		if (player.CanProcess())
		{
			return;
		}

		switch (player)
		{
			case AudioStreamPlayer plain:
				plain.StreamPaused = true;
				break;

			case AudioStreamPlayer2D player2D:
				player2D.StreamPaused = true;
				break;

			case AudioStreamPlayer3D player3D:
				player3D.StreamPaused = true;
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

	// A looping stream plays through to its loop's end once, then repeats the loop: from its start, from its end
	// backward, or back and forth. A seek can't set the direction, so a backward stretch only gets the place right.
	private static double LoopedPosition(AudioStream stream, double position, double length)
	{
		(double begin, double end) = stream switch
		{
			AudioStreamWav wav => (
				(double)wav.LoopBegin / wav.MixRate,
				wav.LoopEnd > wav.LoopBegin ? (double)wav.LoopEnd / wav.MixRate : length),
			AudioStreamOggVorbis ogg => (ogg.LoopOffset, length),
			AudioStreamMP3 mp3 => (mp3.LoopOffset, length),
			_ => (0, length),
		};

		double loop = end - begin;

		if (position < end || loop <= 0)
		{
			return position;
		}

		double past = position - end;

		switch ((stream as AudioStreamWav)?.LoopMode)
		{
			case AudioStreamWav.LoopModeEnum.Backward:
				return end - (past % loop);

			case AudioStreamWav.LoopModeEnum.Pingpong:
				past %= 2 * loop;
				return past < loop ? end - past : begin + (past - loop);

			default:
				return begin + (past % loop);
		}
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
