// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that appends nested-resolver elements to the end of a nested array source. Value-typed elements
/// must resolve to the source element type; object-backed sources accept any registered object element type.
/// </summary>
[Tool]
[GlobalClass]
public partial class AppendResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Append";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "append";

	/// <summary>
	/// Gets or sets the nested resolvers producing the elements to append.
	/// </summary>
	[Export]
	public Array<StatescriptResolverResource> Elements { get; set; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether the element section is folded in the editor.
	/// </summary>
	[Export]
	public bool ElementFolded { get; set; } = true;

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (!TryResolveSource(
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray))
		{
			return false;
		}

		if (sourceObjectArray is not null)
		{
			objectArrayResolver = BuildObjectAppend(graph, sourceObjectArray);
			return true;
		}

		valueArrayResolver = BuildValueAppend(graph, sourceValueArray!);
		return true;
	}

	private IObjectArrayResolver BuildObjectAppend(Graph graph, IObjectArrayResolver source)
	{
		var elements = new List<IObjectResolver>(Elements.Count);

		for (int i = 0; i < Elements.Count; i++)
		{
			if (Elements[i] is null)
			{
				continue;
			}

			if (Elements[i].TryBuildObjectResolver(graph, out IObjectResolver? element) && element is not null)
			{
				elements.Add(element);
			}
			else
			{
				GD.PushError(
					$"Statescript: Append resolver element {i} does not produce an object reference for the " +
					$"'{source.ElementType.Name}[]' array. Skipping it.");
			}
		}

		return ArrayResolverResourceUtilities.CreateObjectAppend(source, elements) ?? source;
	}

	private AppendResolver BuildValueAppend(Graph graph, IArrayPropertyResolver source)
	{
		Type elementType = source.ElementType;
		var elements = new List<IPropertyResolver>(Elements.Count);

		for (int i = 0; i < Elements.Count; i++)
		{
			if (Elements[i] is null)
			{
				continue;
			}

			IPropertyResolver element = Elements[i].BuildResolver(graph);

			if (element.ValueType != elementType)
			{
				GD.PushError(
					$"Statescript: Append resolver element {i} produces '{element.ValueType}', which does not match " +
					$"the source element type '{elementType}'. Skipping it.");
				continue;
			}

			elements.Add(element);
		}

		return new AppendResolver(source, [.. elements]);
	}
}
