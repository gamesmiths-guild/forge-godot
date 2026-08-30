// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a value read off a property of a scene node.
/// </summary>
/// <remarks>
/// The read half of the escape hatch, and the counterpart of Set Node Property. It is how a graph branches on something
/// only the scene knows - a door's open flag, a platform's current speed, a game's own exported difficulty - without
/// that concept having to exist in Forge.
/// </remarks>
/// <param name="nodeResolver">Resolves the node to read from.</param>
/// <param name="propertyPath">The property to read, as a path from that node.</param>
/// <param name="valueType">The type to read the property as.</param>
internal sealed class NodePropertyResolver(
	IObjectResolver<Node> nodeResolver,
	string propertyPath,
	InteropValueType valueType) : IPropertyResolver
{
	private readonly NodePropertyReader _reader = new(nodeResolver, propertyPath);
	private readonly InteropValueType _valueType = valueType;

	public Type ValueType => InteropValues.ToClrType(_valueType);

	public Variant128 Resolve(GraphContext graphContext)
	{
		return _reader.TryRead(graphContext, out Variant value)
			? InteropValues.FromGodot(value, _valueType)
			: default;
	}
}
