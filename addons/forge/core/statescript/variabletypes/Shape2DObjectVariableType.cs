// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Object variable type for <see cref="Shape2D"/> references.
/// </summary>
/// <remarks>
/// The 2D twin of <see cref="ShapeObjectVariableType"/>. It is a type of its own rather than a shared "shape" because
/// the physics servers do not mix: handing a 2D query a 3D shape is not a narrower answer, it is no answer at all, and
/// a dropdown that can express the mistake is a dropdown that will.
/// </remarks>
internal sealed class Shape2DObjectVariableType : StatescriptObjectVariableType<Shape2D>
{
	public override string TypeId => "Shape2D";

	public override string DisplayName => "Shape 2D";

	public override string FormatDebugValue(object? value)
	{
		if (value is not Shape2D shape || !GodotObject.IsInstanceValid(shape))
		{
			return "<null>";
		}

		return shape switch
		{
			CircleShape2D circle => $"Circle(r {circle.Radius:0.##})",
			RectangleShape2D rectangle => $"Rect({rectangle.Size.X:0.##}, {rectangle.Size.Y:0.##})",
			CapsuleShape2D capsule => $"Capsule(r {capsule.Radius:0.##}, h {capsule.Height:0.##})",
			_ => shape.GetType().Name,
		};
	}
}
