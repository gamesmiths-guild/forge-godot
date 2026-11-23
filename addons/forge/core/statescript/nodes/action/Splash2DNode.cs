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
/// Action node that blasts from a 2D point for the ability's owner: each entity in reach gets the blast's effects and
/// push, scaled by how close it is.
/// </summary>
/// <remarks>
/// It's simulation: on the server a client's blast meets the other entities where that client was shown them, and the
/// owner's own push, as a rocket jump's, is predicted with the rest of the owner.
/// </remarks>
[StatescriptCategory("Shots")]
public sealed class Splash2DNode : ActionNode
{
	/// <summary>
	/// Input property index for the blast.
	/// </summary>
	public const byte SplashInput = 0;

	/// <summary>
	/// Input property index for where it blasts.
	/// </summary>
	public const byte CenterInput = 1;

	/// <summary>
	/// Output variable index for how many entities it reached.
	/// </summary>
	public const byte HitsOutput = 0;

	private readonly List<SplashHit2D> _hits = [];

	/// <inheritdoc/>
	public override string Description => "Blasts from a point, with effects and a push scaled by distance.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Splash", typeof(ForgeSplashData)));
		inputProperties.Add(new InputProperty("Center", typeof(NumericsVector2)));
		outputVariables.Add(new OutputVariable("Hits", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (ShotNodeIO.Instigator(graphContext) is not ForgeEntity instigator
			|| !graphContext.TryResolveObject(InputProperties[SplashInput].BoundName, out ForgeSplashData? data)
			|| data is null
			|| !graphContext.TryResolve(InputProperties[CenterInput].BoundName, out NumericsVector2 center))
		{
			return;
		}

		_hits.Clear();
		var at = new Vector2(center.X, center.Y);
		ForgeShots.Splash2D(instigator, at, data, _hits);
		ShotNodeIO.Write(graphContext, OutputVariables[HitsOutput], _hits.Count);
		PhysicsDebugDraw2D.FlashPoint(graphContext, at, PhysicsDebugDraw2D.OverlapFoundColor);
	}
}
