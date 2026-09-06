// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the nearest point on the navigation mesh to a point.
/// </summary>
[Tool]
[GlobalClass]
public partial class NavClosestPoint2DResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "NavClosestPoint2D";

	/// <summary>
	/// Gets or sets the nested resolver providing the point to clamp.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Point { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the point section is folded in the editor.
	/// </summary>
	[Export]
	public bool PointFolded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__navclosestpoint2d_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new NavClosestPoint2DResolver(AdaptResolverForExpectedType(
			Point?.BuildResolver(graph)
				?? new VariantResolver(new Variant128(NumericsVector2.Zero), typeof(NumericsVector2)),
			typeof(NumericsVector2)));
	}
}
