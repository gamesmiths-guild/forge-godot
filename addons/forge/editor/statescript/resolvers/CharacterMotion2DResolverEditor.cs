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
/// Resolver editor for the vectors a character body reports about its last move.
/// </summary>
[Tool]
internal sealed partial class CharacterMotion2DResolverEditor : SpatialResolverEditorBase2D
{
	private static readonly string[] _valueNames =
		["Real Velocity", "Floor Normal", "Wall Normal", "Last Motion", "Position Delta", "Platform Velocity"];

	private OptionButton? _valueDropdown;

	public override string DisplayName => "Character Motion 2D";

	public override string ResolverTypeId => "CharacterMotion2D";

	protected override Type ValueClrType => typeof(NumericsVector2);

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Character Motion 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_valueDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase2D? existingResource)
	{
		int selected = existingResource is CharacterMotion2DResolverResource resource ? (int)resource.Value : 0;
		_valueDropdown = BuildEnumRow(root, "Value:", _valueNames, selected);
	}

	protected override SpatialResolverResourceBase2D BuildResource()
	{
		return new CharacterMotion2DResolverResource
		{
			Value = (CharacterMotionValue)(_valueDropdown?.Selected ?? 0),
		};
	}
}
#endif
