// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Cue handler that plays an animation on the target's animation player, one per cue phase.
/// </summary>
/// <remarks>
/// <para>The phases are separate animation names rather than one animation with a mode, because a stun that starts,
/// holds and ends is three different clips and the alternative is three handlers under three cue tags. A phase left
/// empty plays nothing, which is what makes a one-phase cue - a hit reaction on execute - a single filled field.</para>
/// <para>The handler is a scene node registered against a cue tag, so the animation player it drives belongs to
/// whichever entity the cue is applied to, not to the scene the handler sits in.</para>
/// </remarks>
[GlobalClass]
public partial class AnimationCueHandler : ForgeCueHandler
{
	/// <summary>
	/// Gets or sets the path to the animation player, from the node the target lives on. Empty means the target's
	/// first animation player child.
	/// </summary>
	[Export]
	public string PlayerPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the animation played when the cue is applied.
	/// </summary>
	[Export]
	public string ApplyAnimation { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the animation played when the cue is executed.
	/// </summary>
	[Export]
	public string ExecuteAnimation { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the animation played when the cue is removed.
	/// </summary>
	[Export]
	public string RemoveAnimation { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		Play(forgeEntity, ApplyAnimation);
	}

	/// <inheritdoc/>
	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		Play(forgeEntity, ExecuteAnimation);
	}

	/// <inheritdoc/>
	public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
	{
		Play(forgeEntity, RemoveAnimation);
	}

	private void Play(IForgeEntity forgeEntity, string animation)
	{
		if (animation.Length == 0)
		{
			return;
		}

		if (!ForgeEntityBridge.TryGetEntityChild(forgeEntity, PlayerPath, out AnimationPlayer? player))
		{
			WarnOnce(
				"found no AnimationPlayer for its target" +
				(PlayerPath.Length == 0 ? "." : $" at [{PlayerPath}].") +
				" Nothing was played.");
			return;
		}

		// A name the player does not have is left to Godot, which reports it clearly and once per attempt; a cue fires
		// on application rather than every frame, so there is nothing to suppress.
		player.Play(animation);
	}
}
