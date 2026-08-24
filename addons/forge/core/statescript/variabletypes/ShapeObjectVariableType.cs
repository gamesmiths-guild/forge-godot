// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Object variable type for <see cref="Shape3D"/> references.
/// </summary>
/// <remarks>
/// Making a shape a value is what lets one be built once and used in several places: a cone's box authored on entry and
/// swept by every tick of a loop, or a radius that scales with an ability level resolved once into a variable the rest
/// of the graph reads. It is also what lets a shape be handed to a query at all, since node settings carry primitives
/// only.
/// </remarks>
internal sealed class ShapeObjectVariableType : StatescriptObjectVariableType<Shape3D>
{
	public override string TypeId => "Shape3D";

	public override string DisplayName => "Shape 3D";

	public override string FormatDebugValue(object? value)
	{
		if (value is not Shape3D shape || !GodotObject.IsInstanceValid(shape))
		{
			return "<null>";
		}

		return shape switch
		{
			SphereShape3D sphere => $"Sphere(r {sphere.Radius:0.##})",
			BoxShape3D box => $"Box({box.Size.X:0.##}, {box.Size.Y:0.##}, {box.Size.Z:0.##})",
			CapsuleShape3D capsule => $"Capsule(r {capsule.Radius:0.##}, h {capsule.Height:0.##})",
			CylinderShape3D cylinder => $"Cylinder(r {cylinder.Radius:0.##}, h {cylinder.Height:0.##})",
			_ => shape.GetType().Name,
		};
	}
}
