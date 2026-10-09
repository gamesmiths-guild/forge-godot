// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Helpers;

/// <summary>
/// An entity whose abilities work, on a plain node: placed under a 2D or 3D node it stands where that node does, so a
/// graph run as one of its abilities queries that node's physics world.
/// </summary>
internal sealed partial class TestAbilityEntity : Node, IForgeEntity
{
	public EntityAttributes Attributes { get; }

	public EntityTags Tags { get; }

	public EffectsManager EffectsManager { get; }

	public CuesManager CuesManager { get; }

	public EntityAbilities Abilities { get; }

	public EventManager Events { get; }

	public Variables SharedVariables { get; } = new();

	public TestAbilityEntity()
	{
		CuesManager = new CuesManager();
		Attributes = new EntityAttributes(this, []);
		Tags = new EntityTags(new TagContainer(new TagsManager([])));
		EffectsManager = new EffectsManager(this, CuesManager);
		Abilities = new EntityAbilities(this);
		Events = new EventManager();
	}
}
