// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for the velocity of the body an entity lives on.
/// </summary>
[Tool]
internal sealed partial class EntityVelocity3DResolverEditor : SpatialResolverEditorBase3D
{
	public override string DisplayName => "Entity Velocity 3D";

	public override string ResolverTypeId => "EntityVelocity3D";

	protected override Type ValueClrType => typeof(NumericsVector3);

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Entity Velocity 3D";
		return true;
	}

	protected override SpatialResolverResourceBase3D BuildResource()
	{
		return new EntityVelocity3DResolverResource();
	}
}
#endif
