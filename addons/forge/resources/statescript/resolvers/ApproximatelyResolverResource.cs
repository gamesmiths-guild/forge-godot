// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether two numeric values are approximately equal within a tolerance.
/// </summary>
[Tool]
[GlobalClass]
public partial class ApproximatelyResolverResource : BinaryNestedResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Approximately";

	/// <summary>
	/// Gets or sets the maximum absolute difference considered equal.
	/// </summary>
	[Export]
	public double Tolerance { get; set; } = 1e-6;

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "__approx";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new ApproximatelyResolver(leftResolver, rightResolver, Tolerance);
	}
}
