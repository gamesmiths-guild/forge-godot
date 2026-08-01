// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class RemoveOther : ForgeEffectComponent
{
	private bool _removeAllStacks = true;

	[Export]
	public ForgeEffectQuery[] RemoveQueries { get; set; } = [];

	[Export]
	public bool RemoveAllStacks
	{
		get => _removeAllStacks;

		set
		{
			_removeAllStacks = value;
			NotifyPropertyListChanged();
		}
	}

	[Export(PropertyHint.Range, "1,10,1,or_greater")]
	public int StacksToRemove { get; set; } = 1;

	public override IEffectComponent GetComponent()
	{
		return new RemoveOtherEffectComponent(
			Convert(RemoveQueries),
			RemoveAllStacks ? -1 : Mathf.Max(1, StacksToRemove));
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.StacksToRemove && RemoveAllStacks)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}
#endif

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
