// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Object variable type for <see cref="PackedScene"/> references.
/// </summary>
/// <remarks>
/// Making scenes a variable type means a graph can pick which scene to spawn at runtime: hold an array of scenes and
/// index it, or feed it through the core random-element resolver, instead of needing one graph branch per scene.
/// </remarks>
internal sealed class SceneObjectVariableType : StatescriptObjectVariableType<PackedScene>
{
	public override string TypeId => "Scene";

	public override string DisplayName => "Scene";

	public override string FormatDebugValue(object? value)
	{
		if (value is not PackedScene scene)
		{
			return "<null>";
		}

		return string.IsNullOrEmpty(scene.ResourcePath) ? "Scene(embedded)" : scene.ResourcePath;
	}
}
