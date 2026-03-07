// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

/// <summary>
/// A <see cref="ForgeAbilityBehavior"/> implementation that creates a <see cref="GraphAbilityBehavior"/> from a
/// serialized <see cref="StatescriptGraph"/> resource. The graph is built once and cached, then shared across all
/// ability instances using the Flyweight pattern. Each <see cref="GraphAbilityBehavior"/> creates its own
/// <see cref="GraphProcessor"/> with independent <see cref="GraphContext"/> state.
/// </summary>
[Tool]
[GlobalClass]
[Icon("uid://b6yrjb46fluw3")]
public partial class StatescriptAbilityBehavior : ForgeAbilityBehavior
{
	private Graph? _cachedGraph;

	/// <summary>
	/// Gets or sets the Statescript graph resource that defines the ability's behavior.
	/// </summary>
	[Export]
	public StatescriptGraph? Statescript { get; set; }

	/// <inheritdoc/>
	public override IAbilityBehavior GetBehavior()
	{
		if (Statescript is null)
		{
			GD.PushError("StatescriptAbilityBehavior: Statescript is null.");
			throw new System.InvalidOperationException(
				"StatescriptAbilityBehavior requires a valid Statescript assigned.");
		}

		_cachedGraph ??= StatescriptGraphBuilder.Build(Statescript);
		return new GraphAbilityBehavior(_cachedGraph);
	}
}
