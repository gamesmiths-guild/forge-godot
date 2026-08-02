// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class AttributeAccumulator : ForgeEffectComponent
{
	[Export]
	public string? Attribute { get; set; }

	[Export]
	public ForgeTag? MagnitudeTag { get; set; }

	[Export]
	public AccumulationPolicy Policy { get; set; }

	public override IEffectComponent GetComponent()
	{
		Debug.Assert(Attribute is not null, $"{nameof(Attribute)} reference is missing.");
		Debug.Assert(MagnitudeTag is not null, $"{nameof(MagnitudeTag)} reference is missing.");

		return new AttributeAccumulatorEffectComponent(Attribute, MagnitudeTag.GetTag(), Policy);
	}
}
