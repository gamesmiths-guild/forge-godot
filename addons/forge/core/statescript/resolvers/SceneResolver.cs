// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a constant <see cref="PackedScene"/> authored on the graph.
/// </summary>
/// <remarks>
/// Scenes cannot travel as node settings, because settings only carry primitives. Authoring one as a resolver is how a
/// scene node learns which scene to instantiate.
/// </remarks>
/// <param name="scene">The authored scene, or <see langword="null"/> when the picker was left empty.</param>
internal sealed class SceneResolver(PackedScene? scene) : ObjectResolver<PackedScene>
{
	private readonly PackedScene? _scene = scene;

	public override PackedScene? Resolve(GraphContext graphContext)
	{
		return _scene;
	}
}
