// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a capsule for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class CapsuleShape3DResolverEditor : ShapeResolverEditorBase3D
{
	private NestedResolverPicker? _radiusPicker;
	private NestedResolverPicker? _heightPicker;

	public override string DisplayName => "Capsule";

	public override string ResolverTypeId => "CapsuleShape3D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Capsule";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase3D? existingResource)
	{
		var resource = existingResource as CapsuleShape3DResolverResource;

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
		return new CapsuleShape3DResolverResource
		{
			Radius = _radiusPicker?.BuildResource(),
			RadiusFolded = _radiusPicker?.Folded ?? false,
			Height = _heightPicker?.BuildResource(),
			HeightFolded = _heightPicker?.Folded ?? false,
		};
	}
}
#endif
