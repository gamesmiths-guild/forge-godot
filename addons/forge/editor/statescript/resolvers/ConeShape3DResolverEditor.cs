// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a cone for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class ConeShape3DResolverEditor : ShapeResolverEditorBase3D
{
	private NestedResolverPicker? _anglePicker;
	private NestedResolverPicker? _rangePicker;

	public override string DisplayName => "Cone";

	public override string ResolverTypeId => "ConeShape3D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Cone";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase3D? existingResource)
	{
		var resource = existingResource as ConeShape3DResolverResource;

		_anglePicker = AddDimensionRow(
			root,
			"Angle (deg):",
			resource?.Angle,
			resource?.AngleFolded ?? false,
			ResolverEditorCompatibility.FloatOperandExpectedTypes);

		_rangePicker = AddDimensionRow(
			root,
			"Range:",
			resource?.Range,
			resource?.RangeFolded ?? false,
			ResolverEditorCompatibility.FloatOperandExpectedTypes);
	}

	protected override ShapeResolverResourceBase3D BuildResource()
	{
		return new ConeShape3DResolverResource
		{
			Angle = _anglePicker?.BuildResource(),
			AngleFolded = _anglePicker?.Folded ?? false,
			Range = _rangePicker?.BuildResource(),
			RangeFolded = _rangePicker?.Folded ?? false,
		};
	}
}
#endif
