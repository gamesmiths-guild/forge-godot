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
/// Resolver editor for the vectors a character body reports about its last move.
/// </summary>
[Tool]
internal sealed partial class CharacterMotion3DResolverEditor : SpatialResolverEditorBase3D
{
	private static readonly string[] _valueNames =
		["Real Velocity", "Floor Normal", "Wall Normal", "Last Motion", "Position Delta", "Platform Velocity"];

	private OptionButton? _valueDropdown;

	public override string DisplayName => "Character Motion 3D";

	public override string ResolverTypeId => "CharacterMotion3D";

	protected override Type ValueClrType => typeof(NumericsVector3);

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Character Motion 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_valueDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase3D? existingResource)
	{
		int selected = existingResource is CharacterMotion3DResolverResource resource ? (int)resource.Value : 0;
		_valueDropdown = BuildEnumRow(root, "Value:", _valueNames, selected);
	}

	protected override SpatialResolverResourceBase3D BuildResource()
	{
		return new CharacterMotion3DResolverResource
		{
			Value = (CharacterMotionValue)(_valueDropdown?.Selected ?? 0),
		};
	}
}
#endif
