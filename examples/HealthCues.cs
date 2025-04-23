// Copyright © 2025 Gamesmiths Guild.

using System.Globalization;
using Gamesmiths.Forge.Core.Godot;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayCues.Godot;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
public partial class HealthCues : Cue
{
	[Export]
	public required PackedScene FloatingTextScene { get; set; }

	public override void _CueOnExecute(ForgeEntity? forgeEntity, GameplayCueParameters? parameters)
	{
		if (forgeEntity is null || !parameters.HasValue)
		{
			return;
		}

		if (forgeEntity.GetParent() is not Node3D node3D)
		{
			return;
		}

		FloatText floatingText = FloatingTextScene.Instantiate<FloatText>();
		floatingText.SetText(parameters.Value.Magnitude.ToString(CultureInfo.InvariantCulture));

		floatingText.Position = node3D.Position + new Vector3(0, 2, 0);

		AddChild(floatingText);
	}
}
