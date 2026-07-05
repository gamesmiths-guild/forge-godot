// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the object-backed array element currently being iterated by an enclosing array
/// operation, for any registered object variable type (effects, active effect handles, etc.). Entity elements use
/// <see cref="ElementEntityResolverResource"/> instead, which also plugs into entity-typed slots.
/// </summary>
[Tool]
[GlobalClass]
public partial class ElementObjectResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ElementObject";

	/// <summary>
	/// Gets or sets the registered object variable type id of the iterated array's elements.
	/// </summary>
	[Export]
	public string ObjectTypeId { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (!StatescriptObjectVariableTypeRegistry.TryGet(
			ObjectTypeId,
			out StatescriptObjectVariableType? descriptor))
		{
			GD.PushError(
				$"Statescript: Element resolver references unknown object type id '{ObjectTypeId}'.");
			return false;
		}

		Type closedType = typeof(ElementResolver<>).MakeGenericType(descriptor.ClrType);
		objectResolver = (IObjectResolver)Activator.CreateInstance(closedType)!;
		return true;
	}
}
