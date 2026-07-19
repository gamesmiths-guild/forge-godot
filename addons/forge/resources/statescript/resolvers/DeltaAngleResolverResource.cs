// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that computes the shortest signed angle difference between two angles in radians.
/// </summary>
[Tool]
[GlobalClass]
public partial class DeltaAngleResolverResource : BinaryNestedResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "DeltaAngle";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "__deltaangle";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new DeltaAngleResolver(leftResolver, rightResolver);
	}
}
