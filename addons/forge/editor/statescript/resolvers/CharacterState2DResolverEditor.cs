// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for whether a character body is touching the floor, a wall, or a ceiling.
/// </summary>
[Tool]
internal sealed partial class CharacterState2DResolverEditor : SpatialResolverEditorBase2D
{
	private static readonly string[] _stateNames =
		["On Floor", "On Floor Only", "On Wall", "On Wall Only", "On Ceiling", "On Ceiling Only"];

	private OptionButton? _stateDropdown;

	public override string DisplayName => "Character State 2D";

	public override string ResolverTypeId => "CharacterState2D";

	protected override Type ValueClrType => typeof(bool);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Character State 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_stateDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase2D? existingResource)
	{
		int selected = existingResource is CharacterState2DResolverResource resource ? (int)resource.State : 0;
		_stateDropdown = BuildEnumRow(root, "State:", _stateNames, selected);
	}

	protected override SpatialResolverResourceBase2D BuildResource()
	{
		return new CharacterState2DResolverResource
		{
			State = (CharacterStateQuery)(_stateDropdown?.Selected ?? 0),
		};
	}
}
#endif
