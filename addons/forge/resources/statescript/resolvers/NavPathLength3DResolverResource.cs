// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads how far the walk between two points actually is.
/// </summary>
[Tool]
[GlobalClass]
public partial class NavPathLength3DResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "NavPathLength3D";

	/// <summary>
	/// Gets or sets the nested resolver providing where the walk would start.
	/// </summary>
	[Export]
	public StatescriptResolverResource? From { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing where it would end.
	/// </summary>
	[Export]
	public StatescriptResolverResource? To { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the from section is folded in the editor.
	/// </summary>
	[Export]
	public bool FromFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the to section is folded in the editor.
	/// </summary>
	[Export]
	public bool ToFolded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__navpathlength3d_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new NavPathLength3DResolver(BuildPoint(From, graph), BuildPoint(To, graph));
	}

	private static IPropertyResolver BuildPoint(StatescriptResolverResource? resource, Graph graph)
	{
		return AdaptResolverForExpectedType(
			resource?.BuildResolver(graph)
				?? new VariantResolver(new Variant128(NumericsVector3.Zero), typeof(NumericsVector3)),
			typeof(NumericsVector3));
	}
}
