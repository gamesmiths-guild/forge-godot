// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://cu6ncpuumjo20")]
public partial class ForgeEntity : Node, IForgeEntity
{
	/// <summary>
	/// The group every ready <see cref="ForgeEntity"/> joins, so <see cref="ForgeBootstrap"/> can restart their randoms
	/// before any of them advances, and present their abilities once every one of them has updated.
	/// </summary>
	public const string SimulationGroup = "forge_simulation";

	[Export]
	public ForgeTagContainer BaseTags { get; set; } = new();

	[Export]
	public ForgeSharedVariableSet? SharedVariableDefinitions { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the entity is networked: in a session, the server simulates it and every
	/// client holds a mirror of it. Offline it plays as any other entity. A networked scene, one that is spawned
	/// rather than placed in the level, is listed in the network manifest.
	/// </summary>
	[Export]
	public bool Networked { get; set; }

	/// <summary>
	/// Gets or sets the multiplayer peer whose player the entity is: 1 for the server's own, 0 for the world's. Set it
	/// on the server before the entity enters the tree; a client gets the server's.
	/// </summary>
	public int NetworkOwner { get; set; }

	/// <summary>
	/// Gets the id the entity is known by on every machine in a session, or <see cref="NetworkEntityId.None"/> for one
	/// that isn't networked or not in a session.
	/// </summary>
	public NetworkEntityId NetworkId { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether the entity is a client's mirror of one the server simulates: it doesn't advance,
	/// and holds what the server sends.
	/// </summary>
	public bool IsMirror { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether the entity is a client's own, which it predicts: the session advances it ahead
	/// of the server, from this machine's input, and puts it right as the server's word for each tick arrives.
	/// </summary>
	public bool IsPredicted { get; internal set; }

	public EntityAttributes Attributes { get; set; } = null!;

	public EntityTags Tags { get; set; } = null!;

	public EffectsManager EffectsManager { get; set; } = null!;

	public CuesManager CuesManager { get; set; } = null!;

	public EntityAbilities Abilities { get; set; } = null!;

	public EventManager Events { get; set; } = null!;

	public Variables SharedVariables { get; set; } = null!;

	public DeterministicRandom Random { get; private set; } = null!;

	public EntityInput? Input { get; set; }

	public IEntityMover? Mover { get; set; }

	public IEntityStateHook? StateHook { get; set; }

	/// <summary>
	/// Gets the world history that advances this entity, or <see langword="null"/> when the entity advances in its own
	/// physics step.
	/// </summary>
	public ForgeWorldHistory? WorldHistory { get; internal set; }

	// The seed and tick a world history gave the entity as it spawned into its world, before it was ready, so an entity
	// a replay spawns again rolls the way the one spawned first did. Null for an entity that seeds itself.
	internal ulong? SpawnSeed { get; set; }

	internal long SpawnTick { get; set; }

	// The key a graph of an entity a client predicts spawned this one under, which the server registers it with.
	internal SpawnKey? SpawnKey { get; set; }

	// The session replicating the entity, told when the entity is freed.
	internal ForgeNetworkSession? Replicator { get; set; }

	// Set when a world history lets the entity go for good, which it does only once the entity has let go of
	// everything, in limbo, so freeing it doesn't end what already ended.
	internal bool Released { get; set; }

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

		ForgeNetworkSession? session = Networked ? ForgeNetworkSession.Of(this) : null;
		session?.OnEntityEntering(this);

		var random = new DeterministicRandom(SpawnSeed ?? (((ulong)GD.Randi() << 32) | GD.Randi()));

		// Only the random restarts here, so rolls made before the first tick are seeded; the input frame waits for that
		// tick.
		random.BeginTick(SpawnSeed is null ? ForgeManagers.Instance.Clock.Tick : SpawnTick);
		Random = random;
		AddToGroup(SimulationGroup);
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

		// A mirror starts nothing of its own: the server sends what it has. A client's own entity starts once its
		// session predicts it, so an ability its setup activates is refused as on a predicted entity.
		if (!IsMirror && !IsPredicted)
		{
			ApplySetup();
		}

		session?.OnEntityReady(this);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		Abilities.UpdateAbilities(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		// ForgeBootstrap already began this tick for every entity, before any entity advanced. A world history advances
		// its entities itself, the session its predicted ones, and a mirror doesn't advance: only its copies of the
		// server's graphs run.
		if (IsMirror)
		{
			Abilities.FixedUpdateAbilities(delta);
		}
		else if (WorldHistory is null && !IsPredicted)
		{
			EntitySimulation.Advance(this, delta);
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);

		if (what != NotificationPredelete)
		{
			return;
		}

		Replicator?.OnEntityFreed(this);
		bool letGo = WorldHistory?.OnEntityFreed(this) ?? Released;

		// One that never got ready holds nothing.
		if (!letGo && IsNodeReady())
		{
			try
			{
				Teardown(this);
			}
			catch (InvalidOperationException exception)
			{
				// Freed from inside one of its own changes, which a teardown can't interrupt: it runs once that
				// unwinds, with the node gone, as cue handlers and presentation nodes allow for.
				GD.PushWarning(
					$"Forge: {Name} is torn down once the change it was freed in ends ({exception.Message}). Queue " +
					"Free it instead to tear it down while its node is still there.");

				ForgeEntity entity = this;
				Callable.From(() => Teardown(entity)).CallDeferred();
			}
		}
	}

	// A networked entity rolls from the session's seed and its id, so every machine that simulates it rolls the same.
	internal static ulong NetworkSeed(ulong sessionSeed, NetworkEntityId id)
	{
		return unchecked(sessionSeed ^ (id.Value * 0xD1B54A32D192ED03UL));
	}

	// A placed entity, ready before the session started, rolls from its network seed from now on.
	internal void SeedFor(ulong sessionSeed)
	{
		var random = new DeterministicRandom(NetworkSeed(sessionSeed, NetworkId));
		random.BeginTick(ForgeManagers.Instance.Clock.Tick);
		Random = random;
	}

	internal void ApplySetup()
	{
		var effectApplier = new EffectApplier(this);
		effectApplier.ApplyEffects(this, this, this);
	}

	private static void Teardown(ForgeEntity entity)
	{
		EntitySimulation.Teardown(entity);

		// It's out of the group the bootstrap presents, so what its cancelled abilities showed ends here.
		entity.Abilities.PresentAbilities();
	}
}
