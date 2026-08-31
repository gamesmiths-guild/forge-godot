// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for the velocity of the body an entity lives on.
/// </summary>
[Tool]
internal sealed partial class EntityVelocity2DResolverEditor : SpatialResolverEditorBase2D
{
	public override string DisplayName => "Entity Velocity 2D";

	public override string ResolverTypeId => "EntityVelocity2D";

	protected override Type ValueClrType => typeof(NumericsVector2);

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Entity Velocity 2D";
		return true;
	}

	protected override SpatialResolverResourceBase2D BuildResource()
	{
		return new EntityVelocity2DResolverResource();
	}
}
#endif
