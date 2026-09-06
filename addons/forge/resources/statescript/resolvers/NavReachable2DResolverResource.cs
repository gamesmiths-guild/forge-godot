// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reports whether a destination can be walked to from a point.
/// </summary>
[Tool]
[GlobalClass]
public partial class NavReachable2DResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "NavReachable2D";

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
	/// Gets or sets the nested resolver providing how near the path has to land. Unset uses the agent default.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Tolerance { get; set; }

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

	/// <summary>
	/// Gets or sets a value indicating whether the tolerance section is folded in the editor.
	/// </summary>
	[Export]
	public bool ToleranceFolded { get; set; } = true;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph, runtimeNode, $"__navreachable2d_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new NavReachable2DResolver(
			BuildPoint(From, graph),
			BuildPoint(To, graph),
			Tolerance?.BuildResolver(graph));
	}

	private static IPropertyResolver BuildPoint(StatescriptResolverResource? resource, Graph graph)
	{
		return AdaptResolverForExpectedType(
			resource?.BuildResolver(graph)
				?? new VariantResolver(new Variant128(NumericsVector2.Zero), typeof(NumericsVector2)),
			typeof(NumericsVector2));
	}
}
