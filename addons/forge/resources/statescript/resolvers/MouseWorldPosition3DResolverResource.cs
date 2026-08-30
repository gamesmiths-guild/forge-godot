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
[Tool]
[GlobalClass]
public partial class MouseWorldPosition3DResolverResource : ValueResolverResourceBase
{
	/// <summary>
	/// How far the cursor's ray reaches when nothing was authored, matching the aim payload's own default.
	/// </summary>
	private const double DefaultMaxDistance = 1000.0;

	/// <inheritdoc/>
	public override string ResolverTypeId => "MouseWorldPosition3D";

	/// <summary>
	/// Gets or sets how the cursor's ray is turned into a point.
	/// </summary>
	[Export]
	public MouseWorldMode Mode { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the physics layers the ray can hit. Zero means every layer.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Mask { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the mask section is folded in the editor.
	/// </summary>
	[Export]
	public bool MaskFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing how far the ray reaches.
	/// </summary>
	[Export]
	public StatescriptResolverResource? MaxDistance { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the max distance section is folded in the editor.
	/// </summary>
	[Export]
	public bool MaxDistanceFolded { get; set; } = true;

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "mouseworldposition3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		// A nested operand has no unbound state, so an unauthored distance would be zero and every query would resolve
		// onto the camera itself. The editor seeds the row with the same constant this falls back to.
		IPropertyResolver maxDistanceResolver = MaxDistance is null
			? new VariantResolver(new Variant128(DefaultMaxDistance), typeof(double))
			: AdaptResolverForExpectedType(MaxDistance.BuildResolver(graph), typeof(double));

		return new MouseWorldPosition3DResolver(Mode, Mask?.BuildResolver(graph), maxDistanceResolver);
	}
}
