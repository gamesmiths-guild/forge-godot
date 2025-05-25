// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
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

	public EntityAttributes Attributes { get; set; } = null!;

	public EntityTags Tags { get; set; } = null!;

	public EffectsManager EffectsManager { get; set; } = null!;

	public override void _Ready()
	{
		base._Ready();

		Tags = new(BaseTags.GetTagContainer());
		EffectsManager = new EffectsManager(this, ForgeManagers.Instance.CuesManager);

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
