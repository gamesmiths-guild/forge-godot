// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using GodotNode = Godot.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads a property off a scene node.
/// </summary>
[Tool]
[GlobalClass]
public partial class NodePropertyResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "NodeProperty";

	/// <summary>
	/// Gets or sets the nested resolver providing the node to read from.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Node { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the node section is folded in the editor.
	/// </summary>
	[Export]
	public bool NodeFolded { get; set; }

	/// <summary>
	/// Gets or sets the property to read, as a path from the node.
	/// </summary>
	/// <remarks>
	/// A path rather than a name, so <c>position:y</c> reaches into a property as well as at it.
	/// </remarks>
	[Export]
	public string PropertyPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the type to read the property as.
	/// </summary>
	[Export]
	public InteropValueType ValueType { get; set; } = InteropValueType.Float;

	/// <summary>
	/// Gets or sets a value indicating whether the property holds an array of that type.
	/// </summary>
	/// <remarks>
	/// Follows the slot rather than being authored: a resolver in an array input reads an array, and the same one in a
	/// scalar input reads a value.
	/// </remarks>
	[Export]
	public bool IsArray { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__node_property_{nodeId}_{index}");

		if (IsArray)
		{
			if (InteropValues.IsObjectLane(ValueType))
			{
				graph.VariableDefinitions.DefineObjectArrayProperty(
					propertyName,
					new NodePropertyNodeArrayResolver(BuildNodeResolver(graph), PropertyPath));
			}
			else
			{
				graph.VariableDefinitions.DefineArrayProperty(
					propertyName,
					new NodePropertyArrayResolver(BuildNodeResolver(graph), PropertyPath, ValueType));
			}

			runtimeNode.BindInput(index, propertyName);
			return;
		}

		if (InteropValues.IsObjectLane(ValueType))
		{
			graph.VariableDefinitions.DefineObjectProperty(
				propertyName,
				new NodePropertyNodeResolver(BuildNodeResolver(graph), PropertyPath));
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		DefineAndBindInputProperty(graph, runtimeNode, propertyName, index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new NodePropertyResolver(BuildNodeResolver(graph), PropertyPath, ValueType);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (!InteropValues.IsObjectLane(ValueType))
		{
			return false;
		}

		objectResolver = new NodePropertyNodeResolver(BuildNodeResolver(graph), PropertyPath);
		return true;
	}

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (!IsArray)
		{
			return false;
		}

		if (InteropValues.IsObjectLane(ValueType))
		{
			objectArrayResolver = new NodePropertyNodeArrayResolver(BuildNodeResolver(graph), PropertyPath);
			return true;
		}

		valueArrayResolver = new NodePropertyArrayResolver(BuildNodeResolver(graph), PropertyPath, ValueType);
		return true;
	}

	// An unset node reads from nothing rather than from some fallback: a graph that has not said which node to look at
	// should find nobody, not quietly find the caster's. The seeded Node From Entity is what makes "a property on me"
	// the row a fresh picker already shows.
	private IObjectResolver<GodotNode> BuildNodeResolver(Graph graph)
	{
		if (Node is not null
			&& Node.TryBuildObjectResolver(graph, out IObjectResolver? objectResolver)
			&& objectResolver is IObjectResolver<GodotNode> nodeResolver)
		{
			return nodeResolver;
		}

		return new NodePathResolver(string.Empty);
	}
}
