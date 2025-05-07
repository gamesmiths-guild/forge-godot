// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags.Godot;
using Gamesmiths.Forge.Nodes;
using Godot;

using static Gamesmiths.Forge.Godot.Forge;

using ForgeAttributeSet = Gamesmiths.Forge.Core.AttributeSet;

namespace Gamesmiths.Forge.Core.Godot;

public partial class ForgeEntity : Node, IForgeEntity
{
	[Export]
	public TagContainer BaseTags { get; set; } = new();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
	public Attributes Attributes { get; set; }

	public GameplayTags GameplayTags { get; set; }

	public GameplayEffectsManager EffectsManager { get; set; }

	public override void _Ready()
	{
		Debug.Assert(CuesManager is not null, $"{CuesManager} should have been initialized by the Forge plugin.");

		base._Ready();

		GameplayTags = new(BaseTags.GetTagContainer());
		EffectsManager = new GameplayEffectsManager(this, CuesManager);

		List<ForgeAttributeSet> attributeSetList = [];

		foreach (Node node in GetChildren())
		{
			if (node is AttributeSet attributeSetNode)
			{
				ForgeAttributeSet? attributeSet = attributeSetNode.GetAttributeSet();

				if (attributeSet is not null)
				{
					attributeSetList.Add(attributeSet);
				}
			}
		}

		Attributes = new Attributes([.. attributeSetList]);

		var effectApplier = new EffectApplier(this);
		effectApplier.ApplyEffects(this, this);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		EffectsManager.UpdateEffects(delta);
	}
}
