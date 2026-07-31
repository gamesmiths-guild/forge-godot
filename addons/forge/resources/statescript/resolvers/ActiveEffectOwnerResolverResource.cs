// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entity that triggered an active effect — its ownership owner — from an
/// <see cref="ActiveEffectHandle"/> produced by a nested resolver.
/// </summary>
[Tool]
[GlobalClass]
public partial class ActiveEffectOwnerResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ActiveEffectOwner";

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
				"Statescript: Active Effect Owner resolver requires an Active Effect source. Falling back to the " +
				"ability owner.");
			return new AbilityOwnerResolver();
		}

		return new ActiveEffectOwnerResolver(typedHandleResolver);
	}
}
