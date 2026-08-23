// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for the rotation of the node an entity lives on.
/// </summary>
[Tool]
internal sealed partial class Rotation3DResolverEditor : SpatialResolverEditorBase3D
{
	private OptionButton? _spaceDropdown;

	public override string DisplayName => "Rotation 3D";

	public override string ResolverTypeId => "Rotation3D";

	protected override Type ValueClrType => typeof(NumericsQuaternion);

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Rotation 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_spaceDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase3D? existingResource)
	{
		int selected = existingResource is Rotation3DResolverResource resource ? (int)resource.Space : 0;
		_spaceDropdown = BuildEnumRow(root, "Space:", ["Global", "Local"], selected);
	}

	protected override SpatialResolverResourceBase3D BuildResource()
	{
		return new Rotation3DResolverResource
		{
			Space = (TransformSpace)(_spaceDropdown?.Selected ?? 0),
		};
	}
}
#endif
