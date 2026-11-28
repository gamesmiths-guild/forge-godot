// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A weapon's recoil: a shot's cue kicks its shooter's view up, on the machine whose player it is. The view aims the
/// input, so the kick moves the shots that follow it, on the server too.
/// </summary>
public partial class RecoilCueHandler : ForgeCueHandler
{
	/// <summary>
	/// Gets or sets how far a shot kicks the view up, in degrees.
	/// </summary>
	[Export]
	public float Degrees { get; set; } = 1f;

	/// <inheritdoc/>
	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (forgeEntity is Node entity
			&& entity.GetParent().GetNodeOrNull<FirstPersonView>("View") is FirstPersonView view)
		{
			view.Kick(Degrees);
		}
	}
}
