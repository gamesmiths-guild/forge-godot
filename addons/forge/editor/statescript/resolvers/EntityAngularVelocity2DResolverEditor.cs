// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for how fast the body an entity lives on is spinning.
/// </summary>
/// <remarks>
/// Takes the float lane's whole compatibility set rather than the base's single-type check, the same as Entity
/// Rotation 2D and for the same reason: a 2D spin is a number, and the inputs that want one are typed as any of the
/// numeric kinds.
/// </remarks>
[Tool]
internal sealed partial class EntityAngularVelocity2DResolverEditor : SpatialResolverEditorBase2D
{
	public override string DisplayName => "Entity Angular Velocity 2D";

	public override string ResolverTypeId => "EntityAngularVelocity2D";

	protected override Type ValueClrType => typeof(float);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return ResolverEditorCompatibility.IsFloatType(expectedType) || expectedType == typeof(ForgeVariant128);
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Entity Angular Velocity 2D";
		return true;
	}

	protected override SpatialResolverResourceBase2D BuildResource()
	{
		return new EntityAngularVelocity2DResolverResource();
	}
}
#endif
