// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reports whether a body would fit at a destination.
/// </summary>
[Tool]
[GlobalClass]
public partial class CanFit2DResolverResource : SpatialResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CanFit2D";

	/// <summary>
	/// Gets or sets the nested resolver providing where the body would be.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Destination { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the destination section is folded in the editor.
	/// </summary>
	[Export]
	public bool DestinationFolded { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "canfit2d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		// An unset destination falls back to where the body already is, which reports true and is the honest answer
		// for a question that has not been asked yet - a nested operand has no unbound state to mean anything else.
		IPropertyResolver destinationResolver = Destination is null
			? new EntityPosition2DResolver(entityResolver, NodePath, TransformSpace.Global)
			: AdaptResolverForExpectedType(Destination.BuildResolver(graph), typeof(NumericsVector2));

		return new CanFit2DResolver(entityResolver, NodePath, destinationResolver);
	}
}
