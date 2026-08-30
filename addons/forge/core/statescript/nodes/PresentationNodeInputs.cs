// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes;

/// <summary>
/// Reads the inputs a presentation node shares: the player it drives, its optional numeric overrides, and the
/// animation it names.
/// </summary>
/// <remarks>
/// One instance per node, held as a field, because the "already warned" flags belong to the authored node rather than
/// to any one activation of it - the same rule the spatial nodes follow. A node running every frame against an entity
/// with no player would otherwise report it every frame. A missing player and a missing animation are suppressed
/// separately, since an entity can hit both in sequence and one silencing the other would leave the second invisible.
/// </remarks>
/// <param name="nodeName">The node's display name, for the warnings.</param>
/// <param name="playerPath">The authored path to the player, or empty to take the entity's own.</param>
internal sealed class PresentationNodeInputs(string nodeName, string playerPath)
{
	private readonly string _nodeName = nodeName;
	private readonly string _playerPath = playerPath ?? string.Empty;

	private bool _reportedMissingPlayer;
	private bool _reportedMissingAnimation;

	/// <summary>
	/// Reads an optional numeric input, telling "unbound" apart from "bound to zero".
	/// </summary>
	/// <remarks>
	/// A volume, a pitch and a blend all have a meaningful zero, so the zero a missing binding resolves to cannot be
	/// the marker for "not authored". The null is what lets each node keep whatever the player itself was authored
	/// with.
	/// </remarks>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the input.</param>
	/// <returns>The resolved value, or <see langword="null"/> when unbound.</returns>
	public static double? ResolveOptional(GraphContext graphContext, StringKey boundName)
	{
		return boundName != StringKey.Empty && graphContext.TryResolve(boundName, out double value)
			? value
			: null;
	}

	/// <summary>
	/// Finds the animation player for the node's entity input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="entityBoundName">The bound name of the entity input. Unbound means the ability's owner.</param>
	/// <param name="player">When this method returns <see langword="true"/>, the animation player.</param>
	/// <returns><see langword="true"/> if a player was found; <see langword="false"/> otherwise.</returns>
	public bool TryGetAnimationPlayer(
		GraphContext graphContext,
		StringKey entityBoundName,
		[NotNullWhen(true)] out AnimationPlayer? player)
	{
		IForgeEntity? entity = SceneInstantiationInputs.ResolveEntityOrOwner(graphContext, entityBoundName);

		if (ForgeEntityBridge.TryGetEntityChild(entity, _playerPath, out player))
		{
			return true;
		}

		ReportMissingPlayerOnce("AnimationPlayer");
		return false;
	}

	/// <summary>
	/// Finds the audio player for the node's entity input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="entityBoundName">The bound name of the entity input. Unbound means the ability's owner.</param>
	/// <param name="player">When this method returns <see langword="true"/>, the audio player.</param>
	/// <returns><see langword="true"/> if a player was found; <see langword="false"/> otherwise.</returns>
	public bool TryGetAudioPlayer(
		GraphContext graphContext,
		StringKey entityBoundName,
		[NotNullWhen(true)] out Node? player)
	{
		IForgeEntity? entity = SceneInstantiationInputs.ResolveEntityOrOwner(graphContext, entityBoundName);

		if (ForgeEntityBridge.TryGetEntityChild(entity, _playerPath, AudioPlayers.IsPlayer, out player))
		{
			return true;
		}

		ReportMissingPlayerOnce("audio player");
		return false;
	}

	/// <summary>
	/// Plays an animation, reporting once when the player does not have it.
	/// </summary>
	/// <remarks>
	/// The name is checked rather than left to Godot, which pushes an error of its own for every failed play - once per
	/// activation for the state node, and once per frame for anything driving it from a loop.
	/// </remarks>
	/// <param name="player">The animation player to drive.</param>
	/// <param name="animation">The animation to play.</param>
	/// <param name="speed">The playback speed multiplier, or <see langword="null"/> for the player's own.</param>
	/// <param name="blend">The blend time in seconds, or <see langword="null"/> for the player's authored blend.
	/// </param>
	/// <returns><see langword="true"/> if the animation was played; <see langword="false"/> otherwise.</returns>
	public bool TryPlayAnimation(AnimationPlayer player, StringName animation, double? speed, double? blend)
	{
		if (!player.HasAnimation(animation))
		{
			ReportMissingAnimationOnce(player, animation);
			return false;
		}

		// A negative blend is Godot's own "use whatever this player was authored with", which is what an unbound blend
		// input should leave in place.
		player.Play(animation, blend ?? -1, (float)(speed ?? 1), fromEnd: false);
		return true;
	}

	private void ReportMissingPlayerOnce(string what)
	{
		if (_reportedMissingPlayer)
		{
			return;
		}

		_reportedMissingPlayer = true;

		GD.PushWarning(
			$"Statescript: {_nodeName} found no {what} for its entity" +
			(_playerPath.Length == 0 ? "." : $" at [{_playerPath}].") +
			" Nothing was played.");
	}

	private void ReportMissingAnimationOnce(AnimationPlayer player, StringName animation)
	{
		if (_reportedMissingAnimation)
		{
			return;
		}

		_reportedMissingAnimation = true;

		GD.PushWarning(
			$"Statescript: {_nodeName} names the animation [{animation}], which [{player.Name}] does not have. " +
			"Nothing was played.");
	}
}
