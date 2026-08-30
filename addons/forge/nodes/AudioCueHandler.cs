// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Cue handler that plays a sound on the target.
/// </summary>
/// <remarks>
/// <para>Two ways to say what plays. <see cref="PlayerPath"/> drives a player already in the target's scene, which is
/// what a sound designer wants for anything whose bus, attenuation or stream randomization is authored. <see
/// cref="Stream"/> creates a player on the target instead, for the common case where a cue is one sound and adding a
/// node to every entity that can receive it is the only obstacle.</para>
/// <para>A created player matches the target's dimension, so a sound on a 3D character is positional without the cue
/// having to say so. It lives as long as the cue does: a persistent cue's player is freed on removal, and a one-shot
/// cue's frees itself when the sound ends.</para>
/// </remarks>
[GlobalClass]
public partial class AudioCueHandler : ForgeCueHandler
{
	private readonly Dictionary<IForgeEntity, Node> _persistentPlayers = [];

	/// <summary>
	/// Gets or sets the path to an existing audio player, from the node the target lives on. Empty falls back to
	/// <see cref="Stream"/>, and then to the target's first audio player child.
	/// </summary>
	[Export]
	public string PlayerPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the stream to play on a player created for the target. Ignored when <see cref="PlayerPath"/>
	/// resolves.
	/// </summary>
	[Export]
	public AudioStream? Stream { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether removing the cue stops the sound. Off lets a tail finish after the
	/// effect that started it has gone.
	/// </summary>
	[Export]
	public bool StopOnRemove { get; set; } = true;

	/// <summary>
	/// Gets or sets the curve setting the volume from the cue's normalized magnitude, read as a linear gain where one
	/// is full. Unset leaves the player's authored volume alone, which is the usual answer for a player a sound
	/// designer mixed; a curve <em>replaces</em> that volume rather than scaling it, so the cue decides the level
	/// outright and repeated applications cannot walk it up or down.
	/// </summary>
	[Export]
	public Curve? MagnitudeCurve { get; set; }

	/// <inheritdoc/>
	public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		Node? player = ResolveExistingPlayer(forgeEntity);

		if (player is null && Stream is not null)
		{
			player = CreatePlayer(forgeEntity);

			if (player is not null)
			{
				_persistentPlayers[forgeEntity] = player;
			}
		}

		if (player is null)
		{
			ReportNoPlayer();
			return;
		}

		AudioPlayers.TryPlay(player, ResolveVolumeDb(parameters), pitch: null);
	}

	/// <inheritdoc/>
	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		Node? player = ResolveExistingPlayer(forgeEntity);

		if (player is null && Stream is not null)
		{
			player = CreatePlayer(forgeEntity);

			// Nothing removes a cue that only executes, so the player it created has to retire itself.
			player?.Connect(
				AudioStreamPlayer.SignalName.Finished,
				Callable.From(player.QueueFree),
				(uint)ConnectFlags.OneShot);
		}

		if (player is null)
		{
			ReportNoPlayer();
			return;
		}

		AudioPlayers.TryPlay(player, ResolveVolumeDb(parameters), pitch: null);
	}

	/// <inheritdoc/>
	public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
	{
		bool created = _persistentPlayers.Remove(forgeEntity, out Node? player);

		player ??= ResolveExistingPlayer(forgeEntity);

		if (player is null || !IsInstanceValid(player))
		{
			return;
		}

		if (StopOnRemove)
		{
			AudioPlayers.Stop(player);
		}

		// A player the handler created has no owner but the cue, so it goes with it. Stopping is left to the flag
		// above: a tail asked to finish still needs its player to exist while it does.
		if (created)
		{
			player.Connect(
				AudioStreamPlayer.SignalName.Finished,
				Callable.From(player.QueueFree),
				(uint)ConnectFlags.OneShot);

			if (!AudioPlayers.IsPlaying(player))
			{
				player.QueueFree();
			}
		}
	}

	private Node? ResolveExistingPlayer(IForgeEntity forgeEntity)
	{
		if (_persistentPlayers.TryGetValue(forgeEntity, out Node? tracked) && IsInstanceValid(tracked))
		{
			return tracked;
		}

		// A stream of its own means the cue brings its sound with it, so an unrelated player already on the target is
		// not what it asked for; only an explicit path selects one.
		if (PlayerPath.Length == 0 && Stream is not null)
		{
			return null;
		}

		return ForgeEntityBridge.TryGetEntityChild(forgeEntity, PlayerPath, AudioPlayers.IsPlayer, out Node? player)
			? player
			: null;
	}

	private Node? CreatePlayer(IForgeEntity forgeEntity)
	{
		Node player;
		Node parent;

		if (ForgeEntityBridge.TryGetSpatialNode3D(forgeEntity, out Node3D? node3D))
		{
			player = new AudioStreamPlayer3D { Stream = Stream };
			parent = node3D;
		}
		else if (ForgeEntityBridge.TryGetSpatialNode2D(forgeEntity, out Node2D? node2D))
		{
			player = new AudioStreamPlayer2D { Stream = Stream };
			parent = node2D;
		}
		else if (ForgeEntityBridge.TryGetEntityNode(forgeEntity, out Node? entityNode))
		{
			player = new AudioStreamPlayer { Stream = Stream };
			parent = entityNode;
		}
		else
		{
			return null;
		}

		parent.AddChild(player);
		return player;
	}

	private double? ResolveVolumeDb(CueParameters? parameters)
	{
		if (MagnitudeCurve is null)
		{
			return null;
		}

		float gain = Mathf.Max(MagnitudeCurve.Sample(parameters?.NormalizedMagnitude ?? 0), 0.0f);

		// An absolute level rather than an offset, because the volume is written onto the player and reading it back on
		// the next application would compound. Silence has no decibel value, so it is the floor Godot's own volume
		// sliders use.
		return gain > 0 ? Mathf.LinearToDb(gain) : -80.0;
	}

	private void ReportNoPlayer()
	{
		WarnOnce(
			"found no audio player for its target" +
			(PlayerPath.Length == 0 ? " and has no stream to create one with." : $" at [{PlayerPath}].") +
			" Nothing was played.");
	}
}
