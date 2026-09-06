// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the nodes a project put in a Godot group.
/// </summary>
/// <remarks>
/// <para>A group is how a project names a <em>set</em> of nodes assembled by hand across a level: every spawn point,
/// every cover marker, a whole patrol. Node Path names one node and Child Entities walks one hierarchy; neither
/// reaches a set whose members have nothing in common except the label a designer gave them.</para>
/// <para>Every node in the group, whatever each one is - markers, lights, areas. Entities In Node Group is the one to
/// bind when the members carry entities; this is the one for everything else, and it is what feeds a random pick of a
/// spawn point or a For Each over patrol markers.</para>
/// <para><b>Every read asks the tree.</b> Godot keeps a group as a live list, so nodes added and freed are followed
/// without anything having to be invalidated, but the array is built per read - bind this where a graph asks
/// occasionally rather than inside something that asks every tick.</para>
/// </remarks>
/// <param name="groupName">The group to read.</param>
internal sealed class NodesInNodeGroupResolver(string groupName) : ObjectArrayResolver<Node>
{
	private readonly StringName _groupName = groupName;

	public override Node[] ResolveArray(GraphContext graphContext)
	{
		if (_groupName.IsEmpty || Engine.GetMainLoop() is not SceneTree tree)
		{
			return [];
		}

		return [.. tree.GetNodesInGroup(_groupName)];
	}
}
