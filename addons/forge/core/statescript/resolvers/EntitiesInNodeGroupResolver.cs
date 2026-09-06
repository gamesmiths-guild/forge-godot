// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entities a project put in a Godot group.
/// </summary>
/// <remarks>
/// <para>The entity-facing half of Nodes In Node Group, and the one most graphs want: a group is how a level names a
/// set its designer assembled - every guard on this floor, the whole boss escort, the objective markers - and an
/// ability that buffs a party or damages a wave needs the entities, not the nodes carrying them.</para>
/// <para>Members that carry no entity are skipped rather than reported as gaps, so a group mixing marked-up characters
/// with plain scenery reads as just the characters. The lookup is the wider one, matching what the physics queries do
/// with a collider: a group holds whatever a designer put in it, which is as likely to be a hurtbox or a marker nested
/// under a character as the character's own node, and walking up finds the entity from any of them.</para>
/// <para>A set rather than a list, because a project that put both a body and its hurtbox in one group named two nodes
/// and one entity - reporting it twice would make a For Each over the group apply everything to it twice.</para>
/// </remarks>
/// <param name="groupName">The group to read.</param>
internal sealed class EntitiesInNodeGroupResolver(string groupName) : ObjectArrayResolver<IForgeEntity>
{
	private readonly StringName _groupName = groupName;
	private readonly HashSet<IForgeEntity> _found = [];

	public override IForgeEntity[] ResolveArray(GraphContext graphContext)
	{
		if (_groupName.IsEmpty || Engine.GetMainLoop() is not SceneTree tree)
		{
			return [];
		}

		// Kept between resolves so a repeated read does not allocate the set each time. Forge runs its graphs on one
		// thread, and the contents never outlive the copy returned below.
		_found.Clear();

		foreach (Node node in tree.GetNodesInGroup(_groupName))
		{
			if (ForgeEntityBridge.TryGetEntityInHierarchy(node, out IForgeEntity? entity))
			{
				_found.Add(entity);
			}
		}

		return [.. _found];
	}
}
