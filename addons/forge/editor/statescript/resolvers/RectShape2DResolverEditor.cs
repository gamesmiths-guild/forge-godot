// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a rectangle for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class RectShape2DResolverEditor : ShapeResolverEditorBase2D
{
	private static readonly Type[] _sizeExpectedTypes = [typeof(NumericsVector2)];

	private NestedResolverPicker? _sizePicker;

	public override string DisplayName => "Rectangle";

	public override string ResolverTypeId => "RectShape2D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Rectangle";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase2D? existingResource)
	{
		var resource = existingResource as RectShape2DResolverResource;

		_sizePicker = AddDimensionRow(
			root,
			"Size:",
			resource?.Size,
			resource?.SizeFolded ?? false,
			_sizeExpectedTypes);
	}

	protected override ShapeResolverResourceBase2D BuildResource()
	{
		return new RectShape2DResolverResource
		{
			Size = _sizePicker?.BuildResource(),
			SizeFolded = _sizePicker?.Folded ?? false,
		};
	}
}
#endif
