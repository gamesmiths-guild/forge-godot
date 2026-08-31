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
/// Resolver editor for the rotation of the node an entity lives on.
/// </summary>
/// <remarks>
/// The only spatial getter that reports a plain number, so it takes the float lane's whole compatibility set rather
/// than the base's single-type check: a 2D rotation is an angle, and the inputs that want one are typed as any of the
/// numeric kinds.
/// </remarks>
[Tool]
internal sealed partial class EntityRotation2DResolverEditor : SpatialResolverEditorBase2D
{
	private OptionButton? _spaceDropdown;

	public override string DisplayName => "Entity Rotation 2D";

	public override string ResolverTypeId => "EntityRotation2D";

	protected override Type ValueClrType => typeof(float);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return ResolverEditorCompatibility.IsFloatType(expectedType) || expectedType == typeof(ForgeVariant128);
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Entity Rotation 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_spaceDropdown = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase2D? existingResource)
	{
		int selected = existingResource is EntityRotation2DResolverResource resource ? (int)resource.Space : 0;
		_spaceDropdown = BuildEnumRow(root, "Space:", ["Global", "Local"], selected);
	}

	protected override SpatialResolverResourceBase2D BuildResource()
	{
		return new EntityRotation2DResolverResource
		{
			Space = (TransformSpace)(_spaceDropdown?.Selected ?? 0),
		};
	}
}
#endif
