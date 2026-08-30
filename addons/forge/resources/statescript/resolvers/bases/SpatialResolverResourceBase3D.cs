// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Base resource for every resolver that reads something off the 3D node an entity lives on.
/// </summary>
/// <remarks>
/// Carries the two operands they all share - which entity, and optionally which descendant node of it - so a concrete
/// spatial resolver only declares its own settings and builds its runtime counterpart.
/// </remarks>
[Tool]
public abstract partial class SpatialResolverResourceBase3D : StatescriptResolverResource
{
	/// <summary>
	/// Gets the prefix used to name this resolver's generated graph property.
	/// </summary>
	protected abstract string PropertyNamePrefix { get; }

	/// <summary>
	/// Builds the runtime resolver.
	/// </summary>
	/// <param name="entityResolver">The resolved entity operand, already defaulted to the ability owner.</param>
	/// <param name="graph">The runtime graph being built, for resolvers with nested operands.</param>
	/// <returns>The runtime resolver.</returns>
	protected abstract IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph);

	/// <summary>
	/// Gets or sets which entity to read. Defaults to the ability's owner when left unset.
	/// </summary>
	[Export]
	public StatescriptResolverResource? EntityResolver { get; set; }

	/// <summary>
	/// Gets or sets an optional path to a descendant node to read instead of the entity's own spatial node.
	/// </summary>
	/// <remarks>
	/// Scene-unique names work, so <c>%CastPoint</c> or <c>%Muzzle</c> pointed at a marker node is how an authored
	/// offset is expressed without any code.
	/// </remarks>
	[Export]
	public string NodePath { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__{PropertyNamePrefix}_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return CreateResolver(EntityOperand.BuildOrOwner(EntityResolver, graph), graph);
	}
}
