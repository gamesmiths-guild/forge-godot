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
/// Resolver editor for the position of the node an entity lives on.
/// </summary>
[Tool]
internal sealed partial class Position3DResolverEditor : SpatialResolverEditorBase3D
{
	private OptionButton? _spaceDropdown;

	public override string DisplayName => "Position 3D";

	public override string ResolverTypeId => "Position3D";

	protected override Type ValueClrType => typeof(NumericsVector3);

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Position 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_spaceDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase3D? existingResource)
	{
		int selected = existingResource is Position3DResolverResource resource ? (int)resource.Space : 0;
		_spaceDropdown = BuildEnumRow(root, "Space:", ["Global", "Local"], selected);
	}

	protected override SpatialResolverResourceBase3D BuildResource()
	{
		return new Position3DResolverResource
		{
			Space = (TransformSpace)(_spaceDropdown?.Selected ?? 0),
		};
	}
}
#endif
