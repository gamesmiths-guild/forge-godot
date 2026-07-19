// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that bounces a value back and forth between 0 and a length.
/// </summary>
[Tool]
[GlobalClass]
public partial class PingPongResolverResource : BinaryNestedResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "PingPong";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "__pingpong";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new PingPongResolver(leftResolver, rightResolver);
	}
}
