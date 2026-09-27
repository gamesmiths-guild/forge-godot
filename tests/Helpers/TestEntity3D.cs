// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Helpers;

/// <summary>
/// An entity that is only a place in the scene tree, for nodes that use their entity to find what to move or play: its
/// spatial node and the children under it. None of its gameplay systems exist.
/// </summary>
internal sealed partial class TestEntity3D : Node3D, IForgeEntity
{
	public EntityAttributes Attributes => null!;

	public EntityTags Tags => null!;

	public EffectsManager EffectsManager => null!;

	public CuesManager CuesManager => null!;

	public EntityAbilities Abilities => null!;

	public EventManager Events => null!;

	public Variables SharedVariables => null!;
}
