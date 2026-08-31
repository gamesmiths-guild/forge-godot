// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Node = Godot.Node;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes;

/// <summary>
/// Input reading and output writing shared by the scene nodes.
/// </summary>
/// <remarks>
/// The optional readers return a nullable rather than a zero value, because "no position authored" and "instantiate at
/// the world origin" are different intents and only the first should fall back to the caster's position. The output
/// writer mirrors what core does for its own object outputs, which is internal to that assembly.
/// </remarks>
internal static class SceneInstantiationInputs
{
	/// <summary>
	/// Resolves an entity input, falling back to the ability's owner.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the entity input.</param>
	/// <returns>The resolved entity, or <see langword="null"/>.</returns>
	public static IForgeEntity? ResolveEntityOrOwner(GraphContext graphContext, StringKey boundName)
	{
		if (boundName != StringKey.Empty
			&& graphContext.TryResolveObject(boundName, out IForgeEntity? entity)
			&& entity is not null)
		{
			return entity;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Owner
			: null;
	}

	/// <summary>
	/// Resolves an optional node input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the node input.</param>
	/// <returns>The resolved node, or <see langword="null"/> when unbound.</returns>
	public static Node? ResolveParentNode(GraphContext graphContext, StringKey boundName)
	{
		return boundName != StringKey.Empty
			&& graphContext.TryResolveObject(boundName, out Node? parent)
				? parent
				: null;
	}

	/// <summary>
	/// Resolves an optional vector input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the input.</param>
	/// <returns>The resolved vector, or <see langword="null"/> when unbound.</returns>
	public static NumericsVector3? ResolveOptionalVector3(GraphContext graphContext, StringKey boundName)
	{
		return boundName != StringKey.Empty
			&& graphContext.TryResolve(boundName, out NumericsVector3 value)
				? value
				: null;
	}

	/// <summary>
	/// Resolves an optional 2D vector input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the input.</param>
	/// <returns>The resolved vector, or <see langword="null"/> when unbound.</returns>
	public static NumericsVector2? ResolveOptionalVector2(GraphContext graphContext, StringKey boundName)
	{
		return boundName != StringKey.Empty
			&& graphContext.TryResolve(boundName, out NumericsVector2 value)
				? value
				: null;
	}

	/// <summary>
	/// Resolves an optional rotation input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the input.</param>
	/// <returns>The resolved rotation, or <see langword="null"/> when unbound.</returns>
	public static NumericsQuaternion? ResolveOptionalQuaternion(GraphContext graphContext, StringKey boundName)
	{
		return boundName != StringKey.Empty
			&& graphContext.TryResolve(boundName, out NumericsQuaternion value)
				? value
				: null;
	}

	/// <summary>
	/// Resolves an optional angle input, in radians.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the input.</param>
	/// <returns>The resolved angle, or <see langword="null"/> when unbound.</returns>
	public static double? ResolveOptionalAngle(GraphContext graphContext, StringKey boundName)
	{
		return boundName != StringKey.Empty
			&& graphContext.TryResolve(boundName, out double value)
				? value
				: null;
	}

	/// <summary>
	/// Writes an object to an output variable, honoring its declared scope.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="output">The output variable to write.</param>
	/// <param name="value">The value to write.</param>
	public static void WriteObjectOutput(GraphContext graphContext, OutputVariable output, object? value)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		if (variables?.TryGetObjectVariableType(output.BoundName, out _) == true)
		{
			variables.SetObject(output.BoundName, value);
		}
	}
}
