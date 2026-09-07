// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that holds a constant collision layer or mask, picked as bits rather than typed as a number.
/// </summary>
/// <remarks>
/// The layer space and the fold state are authoring metadata: what this resolver contributes to the graph is the bit
/// field as a plain integer constant, identical to a <see cref="VariantResolverResource"/> holding the same number.
/// </remarks>
[Tool]
[GlobalClass]
public partial class CollisionMaskResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CollisionMask";

	/// <summary>
	/// Gets or sets the selected bits.
	/// </summary>
	[Export]
	public int Value { get; set; }

	/// <summary>
	/// Gets or sets which world's layer names the grid reads.
	/// </summary>
	/// <remarks>
	/// Written by the slot the resolver sits in, and kept here so a mask authored for a graph variable - a slot that
	/// declares no space of its own - still shows the names it was picked with.
	/// </remarks>
	[Export]
	public CollisionLayerSpace LayerSpace { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the grid shows every layer or only the blocks that fit on one row.
	/// </summary>
	[Export]
	public bool Expanded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__collisionmask_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new VariantResolver(new Variant128(Value), typeof(int));
	}
}
