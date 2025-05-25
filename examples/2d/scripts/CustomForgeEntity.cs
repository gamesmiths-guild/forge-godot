// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class CustomForgeEntity : CharacterBody2D, IForgeEntity
{
	[Export]
	public ForgeTagContainer BaseTags { get; set; } = new();

	[Export]
	public float Speed { get; set; } = 1f;

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
			if (node is Godot.Nodes.ForgeAttributeSet attributeSetNode)
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
