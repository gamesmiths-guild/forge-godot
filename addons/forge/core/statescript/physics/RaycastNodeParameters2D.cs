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
/// The inputs and outputs the two 2D ray nodes share, so a one-shot cast and a monitored one ask the same question and
/// report it in the same variables.
/// </summary>
internal static class RaycastNodeParameters2D
{
	/// <summary>
	/// Input property index for where the ray starts.
	/// </summary>
	public const byte OriginInput = 0;

	/// <summary>
	/// Input property index for which way the ray points.
	/// </summary>
	public const byte DirectionInput = 1;

	/// <summary>
	/// Input property index for how far the ray reaches.
	/// </summary>
	public const byte MaxDistanceInput = 2;

	/// <summary>
	/// Input property index for the collision mask.
	/// </summary>
	public const byte MaskInput = 3;

	/// <summary>
	/// Input property index for the entities the ray passes through.
	/// </summary>
	public const byte IgnoreInput = 4;

	/// <summary>
	/// Output variable index for where the ray met the surface.
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
	/// Output variable index for how far along the ray the hit is.
	/// </summary>
	public const byte DistanceOutput = 4;

	/// <summary>
	/// Declares the shared ray inputs and outputs.
	/// </summary>
	/// <param name="inputProperties">The input property list to add to.</param>
	/// <param name="outputVariables">The output variable list to add to.</param>
	public static void Define(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Origin", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Direction", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Max Distance", typeof(double)));
		inputProperties.Add(new InputProperty("Mask", typeof(int), IsOptional: true));

		// Not optional, and seeded by the editor with the ability's owner: a ray fired from the caster's own position
		// starts on the caster's own collider, and unbound would mean exactly what an array of the owner already
		// spells. Emptying it is how a ray that hits everything is authored.
		inputProperties.Add(new InputProperty("Ignore", typeof(IForgeEntity[])));

		outputVariables.Add(new OutputVariable("Hit Position", typeof(NumericsVector2)));
		outputVariables.Add(new OutputVariable("Hit Normal", typeof(NumericsVector2)));
		outputVariables.Add(new OutputVariable("Hit Entity", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Hit Node", typeof(Node)));
		outputVariables.Add(new OutputVariable("Distance", typeof(double)));
	}

	/// <summary>
	/// Resolves the ray inputs and casts.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputProperties">The node's input properties.</param>
	/// <param name="collideWithAreas">Whether areas count as hits, as well as bodies.</param>
	/// <param name="hitFromInside">Whether a ray starting inside a shape reports that shape.</param>
	/// <param name="exclusions">A scratch array the caller owns and reuses, filled with the RIDs of the ignored
	/// entities' colliders.</param>
	/// <param name="result">What the ray met, when it met anything.</param>
	/// <param name="segment">The segment the ray actually covered: from its origin to the hit, or to its full reach on
	/// a miss. This is what gets drawn, so what is on screen is the query that ran rather than the inputs that went
	/// into it.</param>
	/// <returns><see langword="true"/> if the ray hit something; <see langword="false"/> otherwise.</returns>
	public static bool TryCast(
		GraphContext graphContext,
		InputProperty[] inputProperties,
		bool collideWithAreas,
		bool hitFromInside,
		GodotRidArray exclusions,
		out RaycastResult2D result,
		out RaySegment2D segment)
	{
		result = default;
		segment = default;

		World2D? world = PhysicsQuery2D.ResolveWorld(graphContext);

		if (world is null
			|| !graphContext.TryResolve(inputProperties[OriginInput].BoundName, out NumericsVector2 origin)
			|| !graphContext.TryResolve(inputProperties[DirectionInput].BoundName, out NumericsVector2 direction))
		{
			return false;
		}

		graphContext.TryResolve(inputProperties[MaxDistanceInput].BoundName, out double maxDistance);
		graphContext.TryResolve(inputProperties[MaskInput].BoundName, out int mask);

		graphContext.TryResolveObjectArray(
			inputProperties[IgnoreInput].BoundName,
			typeof(IForgeEntity),
			out object?[]? ignored);

		var from = new Vector2(origin.X, origin.Y);
		var along = new Vector2(direction.X, direction.Y);

		bool hit = PhysicsQuery2D.TryRaycast(
			world,
			from,
			along,
			(float)maxDistance,
			PhysicsQuery2D.ResolveMask(mask),
			collideWithAreas,
			hitFromInside,
			PhysicsQuery2D.TryCollectExclusions(ignored, exclusions) ? exclusions : null,
			out result);

		Vector2 to = from;

		if (hit)
		{
			to = result.Position;
		}
		else if (PhysicsQuery2D.IsCastable(along, (float)maxDistance))
		{
			to = from + (along.Normalized() * (float)maxDistance);
		}

		segment = new RaySegment2D(from, to);

		return hit;
	}

	/// <summary>
	/// Writes what the ray met to the node's output variables, including on a miss.
	/// </summary>
	/// <remarks>
	/// A miss writes the default result rather than leaving the outputs alone, so a graph that reads them after a
	/// missed cast sees nothing rather than the last thing that was hit.
	/// </remarks>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="outputVariables">The node's output variables.</param>
	/// <param name="result">What the ray met.</param>
	public static void WriteOutputs(
		GraphContext graphContext,
		OutputVariable[] outputVariables,
		in RaycastResult2D result)
	{
		WriteVector(
			graphContext,
			outputVariables[HitPositionOutput],
			new NumericsVector2(result.Position.X, result.Position.Y));

		WriteVector(
			graphContext,
			outputVariables[HitNormalOutput],
			new NumericsVector2(result.Normal.X, result.Normal.Y));

		WriteObject(graphContext, outputVariables[HitEntityOutput], result.Entity);
		WriteObject(graphContext, outputVariables[HitNodeOutput], result.Node);
		WriteDouble(graphContext, outputVariables[DistanceOutput], result.Distance);
	}

	private static Variables? ResolveVariables(GraphContext graphContext, OutputVariable output)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return null;
		}

		return output.Scope == VariableScope.Shared ? graphContext.SharedVariables : graphContext.GraphVariables;
	}

	// Variant128 is an untagged union, so a value has to be written as the type the bound variable is read back as.
	// Both writers below therefore take the exact type their output declares, and check the variable exists first:
	// SetVar throws on an unknown name, and a graph can outlive the variable a shared binding names.
	private static void WriteVector(GraphContext graphContext, OutputVariable output, NumericsVector2 value)
	{
		Variables? variables = ResolveVariables(graphContext, output);

		if (variables?.TryGetVariant(output.BoundName, out _) == true)
		{
			variables.SetVar(output.BoundName, value);
		}
	}

	private static void WriteDouble(GraphContext graphContext, OutputVariable output, double value)
	{
		Variables? variables = ResolveVariables(graphContext, output);

		if (variables?.TryGetVariant(output.BoundName, out _) == true)
		{
			variables.SetVar(output.BoundName, value);
		}
	}

	private static void WriteObject(GraphContext graphContext, OutputVariable output, object? value)
	{
		Variables? variables = ResolveVariables(graphContext, output);

		if (variables?.TryGetObjectVariableType(output.BoundName, out _) == true)
		{
			variables.SetObject(output.BoundName, value);
		}
	}
}
