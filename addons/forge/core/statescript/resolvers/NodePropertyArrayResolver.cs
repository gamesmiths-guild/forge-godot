// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves an array read off a property of a scene node.
/// </summary>
/// <param name="nodeResolver">Resolves the node to read from.</param>
/// <param name="propertyPath">The property to read, as a path from that node.</param>
/// <param name="valueType">The type to read each element as.</param>
internal sealed class NodePropertyArrayResolver(
	IObjectResolver<Node> nodeResolver,
	string propertyPath,
	InteropValueType valueType) : IArrayPropertyResolver
{
	private readonly NodePropertyReader _reader = new(nodeResolver, propertyPath);
	private readonly InteropValueType _valueType = valueType;

	public Type ElementType => InteropValues.ToClrType(_valueType);

	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		return _reader.TryRead(graphContext, out Variant value)
			? InteropValues.FromGodotArray(value, _valueType)
			: [];
	}
}
