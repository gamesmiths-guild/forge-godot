// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using System.Globalization;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

[GlobalClass]
public partial class FloatingTextCueHandler3D : CueHandler
{
	private Color _damageColor = new(1, 0, 0);

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	public override void _CueOnExecute(IForgeEntity? forgeEntity, GameplayCueParameters? parameters)
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

		if (parameters.Value.Magnitude < 0)
		{
			floatingText.SetColor(_damageColor);
		}

		floatingText.Position = node3D.Position + new Vector3(0, 2, 0);

		AddChild(floatingText);
	}
}
