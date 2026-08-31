// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a wedge for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class WedgeShape2DResolverEditor : ShapeResolverEditorBase2D
{
	private NestedResolverPicker? _anglePicker;
	private NestedResolverPicker? _rangePicker;

	public override string DisplayName => "Wedge";

	public override string ResolverTypeId => "WedgeShape2D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Wedge";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase2D? existingResource)
	{
		var resource = existingResource as WedgeShape2DResolverResource;

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

	protected override ShapeResolverResourceBase2D BuildResource()
	{
		return new WedgeShape2DResolverResource
		{
			Angle = _anglePicker?.BuildResource(),
			AngleFolded = _anglePicker?.Folded ?? false,
			Range = _rangePicker?.BuildResource(),
			RangeFolded = _rangePicker?.Folded ?? false,
		};
	}
}
#endif
