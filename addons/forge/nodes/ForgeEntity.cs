// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://cu6ncpuumjo20")]
public partial class ForgeEntity : Node, IForgeEntity
{
	[Export]
	public ForgeTagContainer BaseTags { get; set; } = new();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
	public EntityAttributes Attributes { get; set; }

	public EntityTags Tags { get; set; }

	public EffectsManager EffectsManager { get; set; }

	public override void _Ready()
	{
		Debug.Assert(ForgeContext.CuesManager is not null, $"{ForgeContext.CuesManager} should have been initialized by the Forge plugin.");

		base._Ready();

		Tags = new(BaseTags.GetTagContainer());
		EffectsManager = new EffectsManager(this, ForgeContext.CuesManager);

		List<AttributeSet> attributeSetList = [];

		foreach (Node node in GetChildren())
		{
			if (node is ForgeAttributeSet attributeSetNode)
			{
				AttributeSet? attributeSet = attributeSetNode.GetAttributeSet();

				if (attributeSet is not null)
				{
					attributeSetList.Add(attributeSet);
				}
			}
		}

		Attributes = new EntityAttributes([.. attributeSetList]);

		var effectApplier = new EffectApplier(this);
		effectApplier.ApplyEffects(this, this);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		EffectsManager.UpdateEffects(delta);
	}
}
