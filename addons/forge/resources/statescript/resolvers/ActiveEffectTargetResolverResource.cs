// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entity an active effect is applied to, from an <see cref="ActiveEffectHandle"/>
/// produced by a nested resolver.
/// </summary>
[Tool]
[GlobalClass]
public partial class ActiveEffectTargetResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ActiveEffectTarget";

	/// <summary>
	/// Gets or sets the nested resolver providing the active effect handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? ActiveEffect { get; set; }

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		if (ActiveEffect is null
			|| !ActiveEffect.TryBuildObjectResolver(graph, out IObjectResolver? handleResolver)
			|| handleResolver is not IObjectResolver<ActiveEffectHandle> typedHandleResolver)
		{
			GD.PushError(
				"Statescript: Active Effect Target resolver requires an Active Effect source. Falling back to the " +
				"ability owner.");
			return new AbilityOwnerResolver();
		}

		return new ActiveEffectTargetResolver(typedHandleResolver);
	}
}
