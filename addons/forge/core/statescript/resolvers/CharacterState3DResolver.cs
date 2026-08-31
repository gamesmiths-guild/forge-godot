// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves whether a character body is touching the floor, a wall, or a ceiling.
/// </summary>
/// <remarks>
/// <para>This exists because these are <em>methods</em> on <see cref="CharacterBody3D"/> rather than properties, and
/// nothing else in the layer can read a method inside an expression: Node Property reads properties, and Call Method
/// is an Action, which cannot appear where a condition is wanted. Without this resolver a grounded-only ability, an
/// air-only ability, and a wall-jump are all unauthorable without code.</para>
/// <para>The reading is only as good as the game's own movement loop. Godot recomputes these during
/// <see cref="CharacterBody3D.MoveAndSlide"/>, so a body the game has not moved this frame reports what it last found
/// — which is the same answer the game's own code would get, and the right one.</para>
/// <para>Only a <see cref="CharacterBody3D"/> has them. A rigid body is not moved by slides and has no notion of
/// standing on something, so it resolves to false.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="state">Which contact state to report.</param>
internal sealed class CharacterState3DResolver(
	IEntityResolver entityResolver,
	string nodePath,
	CharacterStateQuery state) : SpatialResolverBase3D(entityResolver, nodePath)
{
	private readonly CharacterStateQuery _state = state;

	public override Type ValueType => typeof(bool);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not CharacterBody3D characterBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which has no floor or wall contact to read - only a" +
				" CharacterBody3D does. Resolving to false.");

			return new Variant128(false);
		}

		return new Variant128(_state switch
		{
			CharacterStateQuery.OnFloorOnly => characterBody.IsOnFloorOnly(),
			CharacterStateQuery.OnWall => characterBody.IsOnWall(),
			CharacterStateQuery.OnWallOnly => characterBody.IsOnWallOnly(),
			CharacterStateQuery.OnCeiling => characterBody.IsOnCeiling(),
			CharacterStateQuery.OnCeilingOnly => characterBody.IsOnCeilingOnly(),
			_ => characterBody.IsOnFloor(),
		});
	}
}
