// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class Immunity : ForgeEffectComponent
{
	[Export]
	public ForgeEffectQuery[] ImmunityQueries { get; set; } = [];

	public override IEffectComponent GetComponent()
	{
		return new ImmunityEffectComponent(Convert(ImmunityQueries));
	}

	private static EffectQuery[] Convert(ForgeEffectQuery[] queries)
	{
		List<EffectQuery> converted = [];

		foreach (ForgeEffectQuery query in queries)
		{
			if (query is null)
			{
				continue;
			}

			converted.Add(query.GetEffectQuery());
		}

		return [.. converted];
	}
}
