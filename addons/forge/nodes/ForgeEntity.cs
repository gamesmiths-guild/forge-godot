// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://cu6ncpuumjo20")]
public partial class ForgeEntity : Node, IForgeEntity
{
	[Export]
	public ForgeTagContainer BaseTags { get; set; } = new();

	[Export]
	public ForgeSharedVariableSet? SharedVariableDefinitions { get; set; }

	public EntityAttributes Attributes { get; set; } = null!;

	public EntityTags Tags { get; set; } = null!;

	public EffectsManager EffectsManager { get; set; } = null!;

	public CuesManager CuesManager { get; set; } = null!;

	public EntityAbilities Abilities { get; set; } = null!;

	public EventManager Events { get; set; } = null!;

	public Variables SharedVariables { get; set; } = null!;

	/// <summary>
	/// Initializes a new instance of the <see cref="ForgeEntity"/> class.
	/// </summary>
	/// <remarks>
	/// The fixed rail is dispatched ahead of the body this node hangs under, because Godot runs a parent's physics
	/// callback before its children's and the supported layout puts this node inside the character. A node that writes
	/// a velocity for the body to consume - Nav Move To - would otherwise write it after the body had already moved.
	/// Set in the constructor rather than on ready, so a scene that authors its own priority still wins.
	/// </remarks>
	public ForgeEntity()
	{
		ProcessPhysicsPriority = -1;
	}

	public override void _Ready()
	{
		base._Ready();

		Tags = new(BaseTags.GetTagContainer());
		CuesManager = ForgeManagers.Instance.CuesManager;
		EffectsManager = new EffectsManager(this, CuesManager);
		Abilities = new EntityAbilities(this);
		Events = new EventManager();
		SharedVariables = new Variables();

		SharedVariableDefinitions?.PopulateVariables(SharedVariables);

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

		Attributes = new EntityAttributes(this, [.. attributeSetList]);

		var effectApplier = new EffectApplier(this);
		effectApplier.ApplyEffects(this, this, this);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		EffectsManager.UpdateEffects(delta);
		Abilities.UpdateAbilities(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		Abilities.FixedUpdateAbilities(delta);
	}
}
