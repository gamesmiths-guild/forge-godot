// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Shots;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that fires a 2D hitscan from the ability's owner: its pellets hit what is on their lines at once, and
/// its effects go to each entity a pellet hits.
/// </summary>
/// <remarks>
/// <para>It's simulation: the server and the client that predicts the owner fire it alike, pellets spread from the
/// owner's seeded random, and on the server a client's shot meets the other entities where that client was shown them.
/// What it hits shows its cues on the shooter's screen at once.</para>
/// <para>The outputs tell where the first pellet ended, for a tracer: the point it stopped at, or the end of its
/// range.</para>
/// </remarks>
[StatescriptCategory("Shots")]
public sealed class Hitscan2DNode : ActionNode
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

	private readonly List<ShotHit2D> _hits = [];

	/// <inheritdoc/>
	public override string Description =>
		"Fires a hitscan from the ability's owner, applying its effects to what it hits.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Hitscan", typeof(ForgeHitscanData)));
		inputProperties.Add(new InputProperty("Origin", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Aim", typeof(NumericsVector2)));
		outputVariables.Add(new OutputVariable("End", typeof(NumericsVector2)));
		outputVariables.Add(new OutputVariable("Normal", typeof(NumericsVector2)));
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
			|| !graphContext.TryResolve(InputProperties[OriginInput].BoundName, out NumericsVector2 origin)
			|| !graphContext.TryResolve(InputProperties[AimInput].BoundName, out NumericsVector2 aim))
		{
			return;
		}

		_hits.Clear();
		var from = new Vector2(origin.X, origin.Y);
		var along = new Vector2(aim.X, aim.Y);
		ForgeShots.Hitscan2D(instigator, from, along, data, _hits);

		Vector2 end = from + (along.Normalized() * data.Range);
		Vector2 normal = Vector2.Zero;
		Forge.Core.IForgeEntity? entity = null;
		ForgeHitbox2D? hitbox = null;
		int hits = 0;
		bool first = true;

		foreach (ShotHit2D hit in _hits)
		{
			if (first)
			{
				end = hit.Position;
				normal = hit.Normal;
			}

			first = false;
			entity ??= hit.Entity;
			hitbox ??= hit.Hitbox;
			hits += hit.Entity is null ? 0 : 1;
		}

		ShotNodeIO.Write(graphContext, OutputVariables[EndOutput], new NumericsVector2(end.X, end.Y));
		ShotNodeIO.Write(graphContext, OutputVariables[NormalOutput], new NumericsVector2(normal.X, normal.Y));
		SceneInstantiationInputs.WriteObjectOutput(graphContext, OutputVariables[EntityOutput], entity);
		SceneInstantiationInputs.WriteObjectOutput(graphContext, OutputVariables[HitboxOutput], hitbox);
		ShotNodeIO.Write(graphContext, OutputVariables[HitsOutput], hits);
		PhysicsDebugDraw2D.FlashLine(graphContext, from, end, PhysicsDebugDraw2D.RayHitColor);
	}
}
