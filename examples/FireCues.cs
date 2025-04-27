// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core.Godot;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayCues.Godot;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
public partial class FireCues : Cue
{
	private Node3D? _fireEffectSceneInsntace;

	[Export]
	public PackedScene? FireEffectScene { get; set; }

	public override void _CueOnApply(ForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
		base._CueOnApply(forgeEntity, parameters);

		if (forgeEntity is null)
		{
			return;
		}

		if (forgeEntity.GetParent() is not Node3D parent)
		{
			return;
		}

		Debug.Assert(FireEffectScene is not null, $"{nameof(FireEffectScene)} reference is missing.");

		_fireEffectSceneInsntace = FireEffectScene.Instantiate<Node3D>();

		parent.AddChild(_fireEffectSceneInsntace);
		_fireEffectSceneInsntace.Translate(new Vector3(0, 2, 0));
	}

	public override void _CueOnRemove(ForgeEntity forgeEntity, bool interrupted)
	{
		base._CueOnRemove(forgeEntity, interrupted);

		_fireEffectSceneInsntace.Free();
	}
}
