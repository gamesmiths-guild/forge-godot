// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that picks a random element from a nested array source — the "pick a random target" staple.
/// Supports both value-typed and object-backed (entity) sources.
/// </summary>
[Tool]
[GlobalClass]
public partial class RandomElementResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "RandomElement";

	/// <summary>
	/// Gets or sets the nested resolver providing the source array.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Source { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the source section is folded in the editor.
	/// </summary>
	[Export]
	public bool SourceFolded { get; set; } = true;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__randomelement_{nodeId}_{index}");

		if (TryBuildObjectResolver(graph, out IObjectResolver? objectResolver) && objectResolver is not null)
		{
			graph.VariableDefinitions.DefineObjectProperty(propertyName, objectResolver);
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__randomelement_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!ArrayResolverResourceUtilities.TryResolveSource(
			Source,
			ResolverTypeId,
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray)
			|| sourceValueArray is null)
		{
			if (sourceObjectArray is not null)
			{
				GD.PushError(
					"Statescript: Random Element over an object-backed source produces an object value and cannot " +
					"be used as a value input.");
			}

			return new VariantResolver(default, typeof(int));
		}

		return new RandomElementResolver(sourceValueArray, new ForgeRandom());
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (!ArrayResolverResourceUtilities.TryResolveSource(
			Source,
			ResolverTypeId,
			graph,
			out _,
			out IObjectArrayResolver? sourceObjectArray)
			|| sourceObjectArray is null)
		{
			return false;
		}

		objectResolver = (IObjectResolver)Activator.CreateInstance(
			typeof(ObjectRandomElementResolver<>).MakeGenericType(sourceObjectArray.ElementType),
			sourceObjectArray,
			new ForgeRandom())!;

		return true;
	}
}
