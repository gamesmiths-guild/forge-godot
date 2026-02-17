// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

/// <summary>
/// A <see cref="ForgeAbilityBehavior"/> implementation that creates a <see cref="GraphAbilityBehavior"/> from a
/// serialized <see cref="StatescriptGraph"/> resource. The graph is built at runtime using reflection-based node
/// instantiation via <see cref="StatescriptGraphBuilder"/>.
/// </summary>
[Tool]
[GlobalClass]
[Icon("uid://b6yrjb46fluw3")]
public partial class StatescriptAbilityBehavior : ForgeAbilityBehavior
{
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

		Graph runtimeGraph = StatescriptGraphBuilder.Build(Statescript);
		return new GraphAbilityBehavior(runtimeGraph);
	}
}
