// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a circle for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class CircleShape2DResolverEditor : ShapeResolverEditorBase2D
{
	private NestedResolverPicker? _radiusPicker;

	public override string DisplayName => "Circle";

	public override string ResolverTypeId => "CircleShape2D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Circle";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase2D? existingResource)
	{
		var resource = existingResource as CircleShape2DResolverResource;

		_radiusPicker = AddDimensionRow(
			root,
			"Radius:",
			resource?.Radius,
			resource?.RadiusFolded ?? false,
			ResolverEditorCompatibility.FloatOperandExpectedTypes);
	}

	protected override ShapeResolverResourceBase2D BuildResource()
	{
		return new CircleShape2DResolverResource
		{
			Radius = _radiusPicker?.BuildResource(),
			RadiusFolded = _radiusPicker?.Folded ?? false,
		};
	}
}
#endif
