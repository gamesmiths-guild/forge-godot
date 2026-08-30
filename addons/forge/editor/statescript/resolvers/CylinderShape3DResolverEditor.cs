// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a cylinder for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class CylinderShape3DResolverEditor : ShapeResolverEditorBase3D
{
	private NestedResolverPicker? _radiusPicker;
	private NestedResolverPicker? _heightPicker;

	public override string DisplayName => "Cylinder";

	public override string ResolverTypeId => "CylinderShape3D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Cylinder";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase3D? existingResource)
	{
		var resource = existingResource as CylinderShape3DResolverResource;

		_radiusPicker = AddDimensionRow(
			root,
			"Radius:",
			resource?.Radius,
			resource?.RadiusFolded ?? false,
			ResolverEditorCompatibility.FloatOperandExpectedTypes);

		_heightPicker = AddDimensionRow(
			root,
			"Height:",
			resource?.Height,
			resource?.HeightFolded ?? false,
			ResolverEditorCompatibility.FloatOperandExpectedTypes);
	}

	protected override ShapeResolverResourceBase3D BuildResource()
	{
		return new CylinderShape3DResolverResource
		{
			Radius = _radiusPicker?.BuildResource(),
			RadiusFolded = _radiusPicker?.Folded ?? false,
			Height = _heightPicker?.BuildResource(),
			HeightFolded = _heightPicker?.Folded ?? false,
		};
	}
}
#endif
