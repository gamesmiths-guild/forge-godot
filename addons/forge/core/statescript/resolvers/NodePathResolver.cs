// Copyright © Gamesmiths Guild.

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
		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			return null;
		}

		Node? node = (tree.CurrentScene ?? tree.Root)?.GetNodeOrNull(_nodePath);

		if (node is null)
		{
			ReportMissingNodeOnce();
		}

		return node;
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
