// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a shape authored as a resource.
/// </summary>
/// <remarks>
/// The escape hatch from the four built shapes: a convex hull, a height map, or a primitive whose dimensions never
/// change and are easier to set once in the inspector. Nothing here scales at runtime, which is the trade for being
/// able to author any shape Godot has.
/// </remarks>
/// <param name="shape">The authored shape, or <see langword="null"/> when the picker was left empty.</param>
internal sealed class ShapePicker3DResolver(Shape3D? shape) : ObjectResolver<Shape3D>
{
	private readonly Shape3D? _shape = shape;

	public override Shape3D? Resolve(GraphContext graphContext)
	{
		return _shape;
	}
}
