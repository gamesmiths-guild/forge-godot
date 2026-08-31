// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using Node = Godot.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// The inputs and outputs the two sweep nodes share, so a one-shot cast and a monitored one ask the same question and
/// report it in the same variables.
/// </summary>
/// <remarks>
/// The ray family's counterpart, and deliberately the same five outputs: a sweep answers exactly what a ray answers,
/// with a volume instead of a line, so a graph swapped from one to the other keeps every binding it had. The rotation
/// is an angle rather than a quaternion, and is read as authored - an unfilled angle is zero, which means unturned.
/// </remarks>
internal static class ShapecastNodeParameters2D
{
	/// <summary>
	/// Input property index for the shape to sweep.
	/// </summary>
	public const byte ShapeInput = 0;

	/// <summary>
	/// Input property index for where the sweep starts.
	/// </summary>
	public const byte OriginInput = 1;

	/// <summary>
	/// Input property index for which way it goes.
	/// </summary>
	public const byte DirectionInput = 2;

	/// <summary>
	/// Input property index for how far it reaches.
	/// </summary>
	public const byte MaxDistanceInput = 3;

	/// <summary>
	/// Input property index for how the shape is turned, in radians.
	/// </summary>
	public const byte RotationInput = 4;

	/// <summary>
	/// Input property index for the collision mask.
	/// </summary>
	public const byte MaskInput = 5;

	/// <summary>
	/// Input property index for the entities the sweep passes through.
	/// </summary>
	public const byte IgnoreInput = 6;

	/// <summary>
	/// Output variable index for where the sweep met the surface.
	/// </summary>
	public const byte HitPositionOutput = 0;

	/// <summary>
	/// Output variable index for the surface normal at the hit.
	/// </summary>
	public const byte HitNormalOutput = 1;

	/// <summary>
	/// Output variable index for the entity that was hit.
	/// </summary>
	public const byte HitEntityOutput = 2;

	/// <summary>
	/// Output variable index for the collider that was hit.
	/// </summary>
	public const byte HitNodeOutput = 3;

	/// <summary>
	/// Output variable index for how far the sweep got before it was stopped.
	/// </summary>
	public const byte DistanceOutput = 4;

	/// <summary>
	/// Declares the shared sweep inputs and outputs.
	/// </summary>
	/// <param name="inputProperties">The input property list to add to.</param>
	/// <param name="outputVariables">The output variable list to add to.</param>
	public static void Define(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Shape", typeof(Shape2D)));
		inputProperties.Add(new InputProperty("Origin", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Direction", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Max Distance", typeof(double)));
		inputProperties.Add(new InputProperty("Rotation", typeof(double), IsOptional: true));
		inputProperties.Add(new InputProperty("Mask", typeof(int), IsOptional: true));

		// Not optional, and seeded by the editor with the ability's owner, matching the ray nodes. A volume swept from
		// the caster's own position overlaps the caster's own collider from the first instant, which without the
		// exclusion stops the sweep at zero distance every time.
		inputProperties.Add(new InputProperty("Ignore", typeof(IForgeEntity[])));

		outputVariables.Add(new OutputVariable("Hit Position", typeof(NumericsVector2)));
		outputVariables.Add(new OutputVariable("Hit Normal", typeof(NumericsVector2)));
		outputVariables.Add(new OutputVariable("Hit Entity", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Hit Node", typeof(Node)));
		outputVariables.Add(new OutputVariable("Distance", typeof(double)));
	}

	/// <summary>
	/// Resolves the sweep inputs and casts.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputProperties">The node's input properties.</param>
	/// <param name="collideWithAreas">Whether areas stop the sweep, as well as bodies.</param>
	/// <param name="exclusions">A scratch array the caller owns and reuses, filled with the RIDs of the ignored
	/// entities' colliders.</param>
	/// <param name="shape">The shape that was swept, when one resolved. Handed back so the caller can draw it.</param>
	/// <param name="result">What stopped the sweep, when something did.</param>
	/// <param name="hitTransform">Where the shape came to rest. This is what gets drawn, so what is on screen is the
	/// query that ran rather than the inputs that went into it.</param>
	/// <param name="segment">The segment representing the full reach of the sweep.</param>
	/// <returns><see langword="true"/> if the sweep met something; <see langword="false"/> otherwise.</returns>
	public static bool TryCast(
		GraphContext graphContext,
		InputProperty[] inputProperties,
		bool collideWithAreas,
		GodotRidArray exclusions,
		out Shape2D? shape,
		out RaycastResult2D result,
		out Transform2D hitTransform,
		out RaySegment2D segment)
	{
		result = default;
		hitTransform = Transform2D.Identity;
		segment = default;
		shape = null;

		World2D? world = PhysicsQuery2D.ResolveWorld(graphContext);

		if (world is null
			|| !graphContext.TryResolveObject(inputProperties[ShapeInput].BoundName, out shape)
			|| shape is null
			|| !GodotObject.IsInstanceValid(shape)
			|| !graphContext.TryResolve(inputProperties[OriginInput].BoundName, out NumericsVector2 origin)
			|| !graphContext.TryResolve(inputProperties[DirectionInput].BoundName, out NumericsVector2 direction))
		{
			return false;
		}

		graphContext.TryResolve(inputProperties[MaxDistanceInput].BoundName, out double maxDistance);
		graphContext.TryResolve(inputProperties[MaskInput].BoundName, out int mask);
		graphContext.TryResolve(inputProperties[RotationInput].BoundName, out double rotation);

		graphContext.TryResolveObjectArray(
			inputProperties[IgnoreInput].BoundName,
			typeof(IForgeEntity),
			out object?[]? ignored);

		var from = new Vector2(origin.X, origin.Y);
		var along = new Vector2(direction.X, direction.Y);

		hitTransform = new Transform2D((float)rotation, from);

		if (!PhysicsQuery2D.IsCastable(along, (float)maxDistance))
		{
			return false;
		}

		Vector2 motion = along.Normalized() * (float)maxDistance;

		// The full reach, not the part that was travelled. What the sweep was stopped short by is the gap between the
		// end of this segment and the shape drawn where it came to rest.
		segment = new RaySegment2D(from, from + motion);

		return PhysicsQuery2D.TryShapecast(
			world,
			shape,
			hitTransform,
			motion,
			PhysicsQuery2D.ResolveMask(mask),
			collideWithAreas,
			PhysicsQuery2D.TryCollectExclusions(ignored, exclusions) ? exclusions : null,
			out hitTransform,
			out result);
	}

	/// <summary>
	/// Writes what the sweep met to the node's output variables, including on a miss.
	/// </summary>
	/// <remarks>
	/// A miss writes the default result rather than leaving the outputs alone, so a graph that reads them after a
	/// missed sweep sees nothing rather than the last thing that was hit.
	/// </remarks>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="outputVariables">The node's output variables.</param>
	/// <param name="result">What the sweep met.</param>
	public static void WriteOutputs(
		GraphContext graphContext,
		OutputVariable[] outputVariables,
		in RaycastResult2D result)
	{
		RaycastNodeParameters2D.WriteOutputs(graphContext, outputVariables, result);
	}
}
