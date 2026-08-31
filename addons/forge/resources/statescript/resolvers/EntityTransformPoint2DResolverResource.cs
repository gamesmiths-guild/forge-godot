// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that converts a point between an entity's local space and world space.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityTransformPoint2DResolverResource : SpatialResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityTransformPoint2D";

	/// <summary>
	/// Gets or sets the nested resolver providing the point to convert.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Offset { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to convert world to local instead of local to world.
	/// </summary>
	[Export]
	public bool Inverse { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the offset section is folded in the editor.
	/// </summary>
	[Export]
	public bool OffsetFolded { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "entitytransformpoint2d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		IPropertyResolver offsetResolver = Offset?.BuildResolver(graph)
			?? new VariantResolver(new Variant128(NumericsVector2.Zero), typeof(NumericsVector2));

		return new EntityTransformPoint2DResolver(entityResolver, NodePath, offsetResolver, Inverse);
	}
}
