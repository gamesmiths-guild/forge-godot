// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

using static Gamesmiths.Forge.Godot.Core.Forge;

using ForgeAttributeSet = Gamesmiths.Forge.Core.AttributeSet;
using ForgeGameplayTags = Gamesmiths.Forge.Core.GameplayTags;

namespace Gamesmiths.Forge.Example;

public partial class CustomForgeEntity : CharacterBody2D, IForgeEntity
{
	[Export]
	public TagContainer BaseTags { get; set; } = new();

	[Export]
	public float Speed { get; set; } = 1f;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
	public Attributes Attributes { get; set; }

	public ForgeGameplayTags GameplayTags { get; set; }

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
			if (node is Godot.Nodes.AttributeSet attributeSetNode)
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

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (Input.IsActionJustPressed("move_right"))
		{
			MoveAndCollide(new Vector2(1, 0) * Speed * 20);
			EffectsManager.UpdateEffects(1f);
		}
		else if (Input.IsActionJustPressed("move_left"))
		{
			MoveAndCollide(new Vector2(-1, 0) * Speed * 20);
			EffectsManager.UpdateEffects(1f);
		}
		else if (Input.IsActionJustPressed("move_up"))
		{
			MoveAndCollide(new Vector2(0, -1) * Speed * 20);
			EffectsManager.UpdateEffects(1f);
		}
		else if (Input.IsActionJustPressed("move_down"))
		{
			MoveAndCollide(new Vector2(0, 1) * Speed * 20);
			EffectsManager.UpdateEffects(1f);
		}
		else if (Input.IsActionJustPressed("wait"))
		{
			EffectsManager.UpdateEffects(1f);
		}
	}
}
