// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Shots;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that launches a 2D lightweight projectile from the ability's owner.
/// </summary>
/// <remarks>
/// <para>It's simulation: the server and the client that predicts the owner launch it alike, and the owner's ticks fly
/// it, so it leaves the shooter's hand at once and nothing is sent while it flies. Other clients are sent the launch.
/// </para>
/// <para>For a projectile that needs effects, tags or a graph of its own, spawn an entity with Instantiate Scene 2D
/// instead.</para>
/// </remarks>
[StatescriptCategory("Shots")]
public sealed class LaunchProjectile2DNode : ActionNode
{
	/// <summary>
	/// Input property index for the projectile.
	/// </summary>
	public const byte ProjectileInput = 0;

	/// <summary>
	/// Input property index for where it starts.
	/// </summary>
	public const byte OriginInput = 1;

	/// <summary>
	/// Input property index for where it flies.
	/// </summary>
	public const byte DirectionInput = 2;

	/// <summary>
	/// Input property index for how fast, in units per second. Unbound uses the projectile's speed.
	/// </summary>
	public const byte SpeedInput = 3;

	/// <inheritdoc/>
	public override string Description => "Launches a lightweight projectile from the ability's owner.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Projectile", typeof(ForgeProjectileData)));
		inputProperties.Add(new InputProperty("Origin", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Direction", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Speed", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (ShotNodeIO.Instigator(graphContext) is not ForgeEntity instigator
			|| !graphContext.TryResolveObject(InputProperties[ProjectileInput].BoundName, out ForgeProjectileData? data)
			|| data is null
			|| !graphContext.TryResolve(InputProperties[OriginInput].BoundName, out NumericsVector2 origin)
			|| !graphContext.TryResolve(InputProperties[DirectionInput].BoundName, out NumericsVector2 direction))
		{
			return;
		}

		double? speed = PresentationNodeInputs.ResolveOptional(graphContext, InputProperties[SpeedInput].BoundName);

		ForgeShots.Launch2D(
			instigator,
			data,
			new Vector2(origin.X, origin.Y),
			new Vector2(direction.X, direction.Y),
			(float?)speed);
	}
}
