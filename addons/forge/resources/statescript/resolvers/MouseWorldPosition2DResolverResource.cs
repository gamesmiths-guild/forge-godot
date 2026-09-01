// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the point in the world the mouse cursor is over.
/// </summary>
/// <remarks>
/// It has none of the settings its 3D twin carries. A 2D cursor is already a point on the plane the game is played on,
/// so there is no mode to pick between, no ray to mask, and no reach to limit.
/// </remarks>
[Tool]
[GlobalClass]
public partial class MouseWorldPosition2DResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "MouseWorldPosition2D";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "mouseworldposition2d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new MouseWorldPosition2DResolver();
	}
}
