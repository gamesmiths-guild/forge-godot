// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a sphere for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class SphereShape3DResolverEditor : ShapeResolverEditorBase3D
{
	private NestedResolverPicker? _radiusPicker;

	public override string DisplayName => "Sphere";

	public override string ResolverTypeId => "SphereShape3D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Sphere";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase3D? existingResource)
	{
		var resource = existingResource as SphereShape3DResolverResource;

		_radiusPicker = AddDimensionRow(
			root,
			"Radius:",
			resource?.Radius,
			resource?.RadiusFolded ?? false,
			ResolverEditorCompatibility.FloatOperandExpectedTypes);
	}

	protected override ShapeResolverResourceBase3D BuildResource()
	{
		return new SphereShape3DResolverResource
		{
			Radius = _radiusPicker?.BuildResource(),
			RadiusFolded = _radiusPicker?.Folded ?? false,
		};
	}
}
#endif
