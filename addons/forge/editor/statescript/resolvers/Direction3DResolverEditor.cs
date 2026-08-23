// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for a facing direction of the node an entity lives on.
/// </summary>
[Tool]
internal sealed partial class Direction3DResolverEditor : SpatialResolverEditorBase3D
{
	private static readonly string[] _axisNames = ["Forward", "Back", "Right", "Left", "Up", "Down"];

	private OptionButton? _axisDropdown;

	public override string DisplayName => "Direction 3D";

	public override string ResolverTypeId => "Direction3D";

	protected override Type ValueClrType => typeof(NumericsVector3);

	public override bool TryGetInlineSummary(out string summary)
	{
		int index = _axisDropdown is not null && IsInstanceValid(_axisDropdown) ? _axisDropdown.Selected : 0;
		summary = $"Direction 3D ({_axisNames[Math.Clamp(index, 0, _axisNames.Length - 1)]})";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_axisDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase3D? existingResource)
	{
		int selected = existingResource is Direction3DResolverResource resource ? (int)resource.Axis : 0;
		_axisDropdown = BuildEnumRow(root, "Axis:", _axisNames, selected);
	}

	protected override SpatialResolverResourceBase3D BuildResource()
	{
		return new Direction3DResolverResource
		{
			Axis = (SpatialAxis)(_axisDropdown?.Selected ?? 0),
		};
	}
}
#endif
