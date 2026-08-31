// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the nodes held by an array property of another scene node.
/// </summary>
/// <param name="nodeResolver">Resolves the node to read from.</param>
/// <param name="propertyPath">The property to read, as a path from that node.</param>
internal sealed class NodePropertyNodeArrayResolver(IObjectResolver<Node> nodeResolver, string propertyPath)
	: ObjectArrayResolver<Node>
{
	private readonly NodePropertyReader _reader = new(nodeResolver, propertyPath);

	public override Node[] ResolveArray(GraphContext graphContext)
	{
		if (!_reader.TryRead(graphContext, out Variant value))
		{
			return [];
		}

		object?[] values = InteropValues.ObjectFromGodotArray(value);
		var nodes = new Node[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			nodes[i] = (values[i] as Node)!;
		}

		return nodes;
	}
}
