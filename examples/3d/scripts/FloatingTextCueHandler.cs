// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using System.Globalization;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
public partial class FloatingTextCueHandler : CueHandler
{
	private Color _damageColor = new(1, 0, 0);

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

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

		Debug.Assert(FloatingTextScene is not null, $"{nameof(FloatingTextScene)} reference is missing.");

		FloatText floatingText = FloatingTextScene.Instantiate<FloatText>();
		floatingText.SetText(parameters.Value.Magnitude.ToString(CultureInfo.InvariantCulture));

		if (parameters.Value.Magnitude < 0)
		{
			floatingText.SetColor(_damageColor);
		}

		floatingText.Position = node3D.Position + new Vector3(0, 2, 0);

		AddChild(floatingText);
	}
}
