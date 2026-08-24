// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that builds a box for a query to sweep.
/// </summary>
[Tool]
internal sealed partial class BoxShape3DResolverEditor : ShapeResolverEditorBase3D
{
	private static readonly Type[] _sizeExpectedTypes = [typeof(NumericsVector3)];

	private NestedResolverPicker? _sizePicker;

	public override string DisplayName => "Box";

	public override string ResolverTypeId => "BoxShape3D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Box";
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase3D? existingResource)
	{
		var resource = existingResource as BoxShape3DResolverResource;

		_sizePicker = AddDimensionRow(
			root,
			"Size:",
			resource?.Size,
			resource?.SizeFolded ?? false,
			_sizeExpectedTypes);
	}

	protected override ShapeResolverResourceBase3D BuildResource()
	{
		return new BoxShape3DResolverResource
		{
			Size = _sizePicker?.BuildResource(),
			SizeFolded = _sizePicker?.Folded ?? false,
		};
	}
}
#endif
