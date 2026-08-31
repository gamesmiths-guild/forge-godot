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
/// Resolver editor for the position of the node an entity lives on.
/// </summary>
[Tool]
internal sealed partial class EntityPosition2DResolverEditor : SpatialResolverEditorBase2D
{
	private OptionButton? _spaceDropdown;

	public override string DisplayName => "Entity Position 2D";

	public override string ResolverTypeId => "EntityPosition2D";

	protected override Type ValueClrType => typeof(NumericsVector2);

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Entity Position 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_spaceDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase2D? existingResource)
	{
		int selected = existingResource is EntityPosition2DResolverResource resource ? (int)resource.Space : 0;
		_spaceDropdown = BuildEnumRow(root, "Space:", ["Global", "Local"], selected);
	}

	protected override SpatialResolverResourceBase2D BuildResource()
	{
		return new EntityPosition2DResolverResource
		{
			Space = (TransformSpace)(_spaceDropdown?.Selected ?? 0),
		};
	}
}
#endif
