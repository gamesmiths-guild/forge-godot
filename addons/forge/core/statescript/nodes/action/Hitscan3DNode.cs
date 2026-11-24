// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Shots;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that fires a hitscan from the ability's owner: its pellets hit what is on their lines at once, and its
/// effects go to each entity a pellet hits.
/// </summary>
/// <remarks>
/// <para>It's simulation: the server and the client that predicts the owner fire it alike, pellets spread from the
/// owner's seeded random, and on the server a client's shot meets the other entities where that client was shown them.
/// What it hits shows its cues on the shooter's screen at once.</para>
/// <para>The outputs tell where the first pellet ended, for a tracer: the point it stopped at, or the end of its
/// range.</para>
/// </remarks>
[StatescriptCategory("Shots")]
public sealed class Hitscan3DNode : ActionNode
{
	/// <summary>
	/// Input property index for the hitscan.
	/// </summary>
	public const byte HitscanInput = 0;

	/// <summary>
	/// Input property index for where the shot starts.
	/// </summary>
	public const byte OriginInput = 1;

	/// <summary>
	/// Input property index for where it aims.
	/// </summary>
	public const byte AimInput = 2;

	/// <summary>
	/// Output variable index for where the first pellet ended.
	/// </summary>
	public const byte EndOutput = 0;

	/// <summary>
	/// Output variable index for the normal of the surface the first pellet stopped on, or zero.
	/// </summary>
	public const byte NormalOutput = 1;

	/// <summary>
	/// Output variable index for the first entity a pellet hit.
	/// </summary>
	public const byte EntityOutput = 2;

	/// <summary>
	/// Output variable index for the hitbox the first pellet that hit one hit.
	/// </summary>
	public const byte HitboxOutput = 3;

	/// <summary>
	/// Output variable index for how many hits on entities the pellets made.
	/// </summary>
	public const byte HitsOutput = 4;

	private readonly List<ShotHit3D> _hits = [];

	/// <inheritdoc/>
	public override string Description =>
		"Fires a hitscan from the ability's owner, applying its effects to what it hits.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Hitscan", typeof(ForgeHitscanData)));
		inputProperties.Add(new InputProperty("Origin", typeof(NumericsVector3)));
		inputProperties.Add(new InputProperty("Aim", typeof(NumericsVector3)));
		outputVariables.Add(new OutputVariable("End", typeof(NumericsVector3)));
		outputVariables.Add(new OutputVariable("Normal", typeof(NumericsVector3)));
		outputVariables.Add(new OutputVariable("Entity", typeof(Forge.Core.IForgeEntity)));
		outputVariables.Add(new OutputVariable("Hitbox", typeof(global::Godot.Node)));
		outputVariables.Add(new OutputVariable("Hits", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (ShotNodeIO.Instigator(graphContext) is not ForgeEntity instigator
			|| !graphContext.TryResolveObject(InputProperties[HitscanInput].BoundName, out ForgeHitscanData? data)
			|| data is null
			|| !graphContext.TryResolve(InputProperties[OriginInput].BoundName, out NumericsVector3 origin)
			|| !graphContext.TryResolve(InputProperties[AimInput].BoundName, out NumericsVector3 aim))
		{
			return;
		}

		_hits.Clear();
		var from = new Vector3(origin.X, origin.Y, origin.Z);
		var along = new Vector3(aim.X, aim.Y, aim.Z);
		ShotHit3D? stop = ForgeShots3D.Hitscan(instigator, from, along, data, _hits);

		Vector3 end = stop?.Position ?? (from + (along.Normalized() * data.Range));
		Vector3 normal = stop?.Normal ?? Vector3.Zero;
		Forge.Core.IForgeEntity? entity = null;
		ForgeHitbox3D? hitbox = null;
		int hits = 0;

		foreach (ShotHit3D hit in _hits)
		{
			entity ??= hit.Entity;
			hitbox ??= hit.Hitbox;
			hits += hit.Entity is null ? 0 : 1;
		}

		ShotNodeIO.Write(
			graphContext,
			OutputVariables[EndOutput],
			new NumericsVector3(end.X, end.Y, end.Z));

		ShotNodeIO.Write(
			graphContext,
			OutputVariables[NormalOutput],
			new NumericsVector3(normal.X, normal.Y, normal.Z));

		SceneInstantiationInputs.WriteObjectOutput(
			graphContext,
			OutputVariables[EntityOutput],
			entity);

		SceneInstantiationInputs.WriteObjectOutput(
			graphContext,
			OutputVariables[HitboxOutput],
			hitbox);

		ShotNodeIO.Write(
			graphContext,
			OutputVariables[HitsOutput],
			hits);

		PhysicsDebugDraw3D.FlashLine(
			graphContext,
			from,
			end,
			PhysicsDebugDraw3D.RayHitColor);
	}
}
