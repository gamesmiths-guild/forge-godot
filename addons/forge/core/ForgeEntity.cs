// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

using static Gamesmiths.Forge.Godot.Forge;

using ForgeAttributeSet = Gamesmiths.Forge.Core.AttributeSet;

namespace Gamesmiths.Forge.Core.Godot;

public partial class ForgeEntity : Node, IForgeEntity
{
	[Export]
	public TagContainer BaseTags { get; set; } = new();

	public Attributes Attributes { get; set; }

	public GameplayTags Tags { get; set; }

	public GameplayEffectsManager EffectsManager { get; set; }

	GameplayTags IForgeEntity.GameplayTags => throw new System.NotImplementedException();

	public override void _Ready()
	{
		base._Ready();

		Tags = new(BaseTags.GetTagContainer());
		EffectsManager = new GameplayEffectsManager(this, CuesManager);

		List<ForgeAttributeSet> attributeSetList = [];

		foreach (Node node in GetChildren())
		{
			if (node is AttributeSet attributeSetNode)
			{
				attributeSetList.Add(attributeSetNode.GetAttributeSet());
			}
		}

		Attributes = new Attributes([.. attributeSetList]);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		EffectsManager.UpdateEffects(delta);
	}
}
