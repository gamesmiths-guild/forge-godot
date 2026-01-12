// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

[GlobalClass]
public partial class FloatingTextCueHandler3D : ForgeCueHandler
{
	private Color _damageColor = new(1, 0, 0);
	private Color _healingColor = new(0, 1, 0);

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	public override void _CueOnExecute(IForgeEntity? forgeEntity, CueParameters? parameters)
	{
		if (forgeEntity is not Node node || !parameters.HasValue)
		{
			return;
		}

		if (node.GetParent() is not Node3D node3D)
		{
			return;
		}

		Debug.Assert(FloatingTextScene is not null, $"{nameof(FloatingTextScene)} reference is missing.");

		FloatText3D floatingText = FloatingTextScene.Instantiate<FloatText3D>();
		floatingText.SetText(parameters.Value.Magnitude.ToString(CultureInfo.InvariantCulture));

		if (parameters.Value.Magnitude <= 0)
		{
			floatingText.SetColor(_damageColor);
		}
		else
		{
			floatingText.SetColor(_healingColor);
		}

		floatingText.Position = node3D.GlobalPosition + new Vector3(0, 3, 0);

		AddChild(floatingText);
	}
}
