// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using System.Globalization;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

[GlobalClass]
public partial class FloatingTextCueHandler2D : CueHandler
{
	private Color _damageColor = new(1, 0, 0);

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	public override void _CueOnExecute(IForgeEntity? forgeEntity, GameplayCueParameters? parameters)
	{
		if (forgeEntity is not Node2D node2D || !parameters.HasValue)
		{
			return;
		}

		Debug.Assert(FloatingTextScene is not null, $"{nameof(FloatingTextScene)} reference is missing.");

		FloatText2D floatingText = FloatingTextScene.Instantiate<FloatText2D>();
		floatingText.SetText(parameters.Value.Magnitude.ToString(CultureInfo.InvariantCulture));

		if (parameters.Value.Magnitude < 0)
		{
			floatingText.SetColor(_damageColor);
		}

		AddChild(floatingText);

		floatingText.Position = node2D.Position + new Vector2(-20, -30);
	}
}
