// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entities an area already in the scene is overlapping.
/// </summary>
[Tool]
[GlobalClass]
public partial class AreaOverlaps3DResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AreaOverlaps3D";

	/// <summary>
	/// Gets or sets which entity owns the area. Defaults to the ability's owner when left unset.
	/// </summary>
	[Export]
	public StatescriptResolverResource? EntityResolver { get; set; }

	/// <summary>
	/// Gets or sets the path to the area, from the entity's spatial node.
	/// </summary>
	[Export]
	public string NodePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether overlapping areas count, as well as bodies.
	/// </summary>
	[Export]
	public bool IncludeAreas { get; set; }

	/// <summary>
	/// Gets or sets the entities left out of the results, normally the area's own owner. Unset leaves out nothing.
	/// </summary>
	[Export]
	public StatescriptResolverResource? IgnoreResolver { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the ignore section is folded in the editor.
	/// </summary>
	[Export]
	public bool IgnoreFolded { get; set; } = true;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		TryBuildArrayResolver(graph, out _, out IObjectArrayResolver? objectArrayResolver);

		var propertyName = new StringKey($"__areaoverlaps3d_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, objectArrayResolver!);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;

		objectArrayResolver = new AreaOverlaps3DResolver(
			EntityOperand.BuildOrOwner(EntityResolver, graph),
			NodePath,
			IncludeAreas,
			IgnoreOperand.Build(IgnoreResolver, graph));

		return true;
	}
}
