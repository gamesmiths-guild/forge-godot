// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the node held by a property of another scene node.
/// </summary>
/// <remarks>
/// A game's own exported node reference - a turret's target, a door's trigger - reaches the graph through here, and
/// Entity From Node turns it into something effects can be applied to.
/// </remarks>
/// <param name="nodeResolver">Resolves the node to read from.</param>
/// <param name="propertyPath">The property to read, as a path from that node.</param>
internal sealed class NodePropertyNodeResolver(IObjectResolver<Node> nodeResolver, string propertyPath)
	: ObjectResolver<Node>
{
	private readonly NodePropertyReader _reader = new(nodeResolver, propertyPath);

	public override Node? Resolve(GraphContext graphContext)
	{
		return _reader.TryRead(graphContext, out Variant value)
			? InteropValues.ObjectFromGodot(value) as Node
			: null;
	}
}
