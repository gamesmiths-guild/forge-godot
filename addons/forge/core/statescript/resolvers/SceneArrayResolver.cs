// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a constant array of <see cref="PackedScene"/> values authored on the graph.
/// </summary>
/// <remarks>
/// Feeding this through the core random-element or element-at resolvers is how one graph spawns one of several scenes
/// without a branch per scene.
/// </remarks>
/// <param name="scenes">The authored scenes. Empty entries are preserved so indices stay stable.</param>
internal sealed class SceneArrayResolver(IReadOnlyList<PackedScene?> scenes) : ObjectArrayResolver<PackedScene>
{
	private readonly IReadOnlyList<PackedScene?> _scenes = scenes;

	public override PackedScene[] ResolveArray(GraphContext graphContext)
	{
		var resolved = new PackedScene[_scenes.Count];

		for (int i = 0; i < _scenes.Count; i++)
		{
			resolved[i] = _scenes[i]!;
		}

		return resolved;
	}
}
