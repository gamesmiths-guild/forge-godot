// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for a facing direction of the node an entity lives on.
/// </summary>
[Tool]
internal sealed partial class EntityDirection2DResolverEditor : SpatialResolverEditorBase2D
{
	private static readonly string[] _axisNames = ["Forward", "Back", "Right", "Left"];

	private OptionButton? _axisDropdown;

	public override string DisplayName => "Entity Direction 2D";

	public override string ResolverTypeId => "EntityDirection2D";

	protected override Type ValueClrType => typeof(NumericsVector2);

	public override bool TryGetInlineSummary(out string summary)
	{
		int index = _axisDropdown is not null && IsInstanceValid(_axisDropdown) ? _axisDropdown.Selected : 0;
		summary = $"Direction 2D ({_axisNames[Math.Clamp(index, 0, _axisNames.Length - 1)]})";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_axisDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase2D? existingResource)
	{
		int selected = existingResource is EntityDirection2DResolverResource resource ? (int)resource.Axis : 0;
		_axisDropdown = BuildEnumRow(root, "Axis:", _axisNames, selected);
	}

	protected override SpatialResolverResourceBase2D BuildResource()
	{
		return new EntityDirection2DResolverResource
		{
			Axis = (SpatialAxis2D)(_axisDropdown?.Selected ?? 0),
		};
	}
}
#endif
