// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reports whether nothing blocks the line between two points.
/// </summary>
[Tool]
[GlobalClass]
public partial class LineOfSight3DResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "LineOfSight3D";

	/// <summary>
	/// Gets or sets the nested resolver providing where the line starts.
	/// </summary>
	[Export]
	public StatescriptResolverResource? From { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing where the line ends.
	/// </summary>
	[Export]
	public StatescriptResolverResource? To { get; set; }

	/// <summary>
	/// Gets or sets the entities the line passes through, normally the ones at its two ends. Unset ignores nothing.
	/// </summary>
	[Export]
	public StatescriptResolverResource? IgnoreResolver { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the ignore section is folded in the editor.
	/// </summary>
	[Export]
	public bool IgnoreFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing the physics layers that block sight. Zero means every layer.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Mask { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the mask section is folded in the editor.
	/// </summary>
	[Export]
	public bool MaskFolded { get; set; } = true;

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
			$"__lineofsight3d_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new LineOfSight3DResolver(
			BuildPoint(From, graph),
			BuildPoint(To, graph),
			IgnoreOperand.Build(IgnoreResolver, graph),
			Mask?.BuildResolver(graph));
	}

	private static IPropertyResolver BuildPoint(StatescriptResolverResource? resource, Graph graph)
	{
		return AdaptResolverForExpectedType(
			resource?.BuildResolver(graph)
				?? new VariantResolver(new Variant128(NumericsVector3.Zero), typeof(NumericsVector3)),
			typeof(NumericsVector3));
	}
}
