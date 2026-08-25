// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a node authored as a path into the running scene.
/// </summary>
/// <remarks>
/// <para>This is the constant of the node lane, the counterpart of the scene picker: without it the only way to fill a
/// node input is to read a variable something else wrote, which leaves nodes that were placed by hand - a projectile
/// container, a spawn marker, a level prop - unreachable from a graph.</para>
/// <para>The path is resolved from the current scene's root each time, so it follows a scene reload and never holds a
/// reference to a freed node. Absolute paths (<c>/root/Main/Props</c>) and scene-unique names (<c>%SpawnPoint</c>)
/// work, the latter for nodes marked unique in the current scene itself.</para>
/// </remarks>
/// <param name="nodePath">The authored path, resolved from the current scene's root.</param>
internal sealed class NodePathResolver(string nodePath) : ObjectResolver<Node>
{
	private readonly string _nodePath = nodePath;

	private bool _reportedMissingNode;

	public override Node? Resolve(GraphContext graphContext)
	{
		Node? node = ResolveFromGraphEntity(graphContext) ?? ResolveFromCurrentScene();

		if (node is null)
		{
			ReportMissingNodeOnce();
		}

		return node;
	}

	// Searched outward from the graph's own entity rather than down from the current scene, because a scene-unique
	// name is registered on the scene root that owns it, and the current scene is often not that root: a game whose
	// menu instantiates levels as children never changes scene, so CurrentScene stays the outer shell for the whole
	// session and knows nothing about the %names inside the level. Walking the caster's ancestors passes through every
	// scene it belongs to, innermost first, which is also the order an author means when two scenes both define a name.
	private Node? ResolveFromGraphEntity(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			|| !ForgeEntityBridge.TryGetEntityNode(abilityContext.Owner, out Node? current))
		{
			return null;
		}

		while (current is not null && GodotObject.IsInstanceValid(current))
		{
			Node? found = current.GetNodeOrNull(_nodePath);

			if (found is not null)
			{
				return found;
			}

			current = current.GetParent();
		}

		return null;
	}

	// The fallback for a graph with no owner in the scene, which is the only case the walk above cannot start from.
	private Node? ResolveFromCurrentScene()
	{
		return Engine.GetMainLoop() is SceneTree tree
			? (tree.CurrentScene ?? tree.Root)?.GetNodeOrNull(_nodePath)
			: null;
	}

	private void ReportMissingNodeOnce()
	{
		// Resolvers run every tick, so a path pointing at nothing would otherwise warn every frame.
		if (_reportedMissingNode)
		{
			return;
		}

		_reportedMissingNode = true;

		GD.PushWarning($"Statescript: no node found at [{_nodePath}]. Resolving to null.");
	}
}
