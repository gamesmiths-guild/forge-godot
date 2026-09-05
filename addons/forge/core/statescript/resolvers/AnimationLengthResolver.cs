// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves how long an animation runs for, in seconds.
/// </summary>
/// <remarks>
/// <para>This is what keeps a graph's timing and an animator's timing from drifting apart. A wind-up authored as a
/// Timer of 0.8 seconds beside a wind-up animation of 0.8 seconds is two numbers that have to be kept equal by hand,
/// and the day the animation is re-timed only one of them changes. Feeding the timer from here means the animation is
/// the only place the number lives.</para>
/// <para>It reads the clip rather than the playback, so it is the same answer whether or not anything is playing -
/// which is what lets it be read at activation, before the animation starts. A graph that also drives playback speed
/// divides by it, since a clip does not know what speed it will be played at.</para>
/// <para>An animation the player does not have resolves to zero, warned once. Zero is a duration a timer can survive,
/// which a graph waiting on a made-up number is not.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity's player to read.</param>
/// <param name="playerPath">Path to the animation player, from the node the entity lives on. Empty means the entity's
/// first animation player child.</param>
/// <param name="animation">The name of the animation to measure.</param>
internal sealed class AnimationLengthResolver(
	IEntityResolver entityResolver,
	string playerPath,
	string animation) : IPropertyResolver
{
	private readonly IEntityResolver _entityResolver = entityResolver;
	private readonly string _playerPath = playerPath ?? string.Empty;
	private readonly StringName _animation = animation ?? string.Empty;

	private bool _reportedMissingPlayer;
	private bool _reportedMissingAnimation;

	public Type ValueType => typeof(double);

	public Variant128 Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (!ForgeEntityBridge.TryGetEntityChild(entity, _playerPath, out AnimationPlayer? player))
		{
			ReportMissingPlayerOnce();
			return new Variant128(0.0);
		}

		if (!player.HasAnimation(_animation))
		{
			ReportMissingAnimationOnce(player);
			return new Variant128(0.0);
		}

		double length = player.GetAnimation(_animation).Length;
		return new Variant128(length);
	}

	// Resolvers run every tick, so a warning left unsuppressed would repeat every frame. A missing player and a
	// missing animation are suppressed separately, since an entity can hit both in sequence and one silencing the
	// other would leave the second invisible.
	private void ReportMissingPlayerOnce()
	{
		if (_reportedMissingPlayer)
		{
			return;
		}

		_reportedMissingPlayer = true;

		GD.PushWarning(
			"Statescript: Animation Length found no AnimationPlayer for its entity" +
			(_playerPath.Length == 0 ? "." : $" at [{_playerPath}].") +
			" Resolving to zero.");
	}

	private void ReportMissingAnimationOnce(AnimationPlayer player)
	{
		if (_reportedMissingAnimation)
		{
			return;
		}

		_reportedMissingAnimation = true;

		GD.PushWarning(
			$"Statescript: Animation Length names the animation [{_animation}], which [{player.Name}] does not " +
			"have. Resolving to zero.");
	}
}
