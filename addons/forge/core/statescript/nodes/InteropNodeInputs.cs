// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes;

/// <summary>
/// Reads the inputs the interop nodes share: which node to act on, and the typed arguments a call or a signal carries.
/// </summary>
internal static class InteropNodeInputs
{
	/// <summary>
	/// Resolves a node input, falling back to the node the ability's owner lives on.
	/// </summary>
	/// <remarks>
	/// The fallback is what makes "a property on me" the unbound case, which is overwhelmingly the common one. It is
	/// the owning node in either dimension rather than the entity's own node, because a composed entity is a plain
	/// child of the body and the body is the thing being reached for.
	/// </remarks>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the node input.</param>
	/// <returns>The resolved node, or <see langword="null"/> when neither is available.</returns>
	public static Node? ResolveNodeOrOwner(GraphContext graphContext, StringKey boundName)
	{
		if (boundName != StringKey.Empty
			&& graphContext.TryResolveObject(boundName, out Node? node)
			&& node is not null
			&& GodotObject.IsInstanceValid(node))
		{
			return node;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			&& ForgeEntityBridge.TryGetOwningNode(abilityContext.Owner, string.Empty, out Node? ownerNode)
				? ownerNode
				: null;
	}

	/// <summary>
	/// Resolves a typed value input as the Godot variant to hand to the scene.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="boundName">The bound name of the value input.</param>
	/// <param name="valueType">The kind the input was declared as.</param>
	/// <param name="isArray">Whether the input holds an array of that kind.</param>
	/// <param name="value">The Godot value, when the input resolved.</param>
	/// <returns><see langword="true"/> when the input resolved.</returns>
	public static bool TryResolveTypedValue(
		GraphContext graphContext,
		StringKey boundName,
		InteropValueType valueType,
		bool isArray,
		out Variant value)
	{
		if (InteropValues.IsObjectLane(valueType))
		{
			if (isArray)
			{
				bool resolvedObjects = graphContext.TryResolveObjectArray(
					boundName,
					typeof(Node),
					out object?[]? objectValues);

				value = resolvedObjects ? InteropValues.ObjectToGodotArray(objectValues!) : default;
				return resolvedObjects;
			}

			bool resolvedObject = graphContext.TryResolveObject(boundName, typeof(Node), out object? objectValue);
			value = resolvedObject ? InteropValues.ObjectToGodot(objectValue) : default;
			return resolvedObject;
		}

		if (isArray)
		{
			bool resolvedValues = graphContext.TryResolveArray(boundName, out Variant128[]? values);
			value = resolvedValues ? InteropValues.ToGodotArray(values!, valueType) : default;
			return resolvedValues;
		}

		bool resolvedValue = graphContext.TryResolveVariant(boundName, out Variant128 scalar);
		value = resolvedValue ? InteropValues.ToGodot(scalar, valueType) : default;
		return resolvedValue;
	}

	/// <summary>
	/// Resolves the arguments a call or an emission passes, in order, stopping at the first one that is not configured.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputProperties">The node's input properties.</param>
	/// <param name="firstArgumentIndex">The input index of the first argument.</param>
	/// <param name="argumentTypes">The configured argument types, in order.</param>
	/// <returns>The arguments to pass.</returns>
	public static Variant[] ResolveArguments(
		GraphContext graphContext,
		InputProperty[] inputProperties,
		byte firstArgumentIndex,
		IReadOnlyList<InteropValueType> argumentTypes)
	{
		var arguments = new List<Variant>(argumentTypes.Count);

		for (int i = 0; i < argumentTypes.Count; i++)
		{
			if (argumentTypes[i] == InteropValueType.None)
			{
				break;
			}

			arguments.Add(ResolveArgument(
				graphContext,
				inputProperties[firstArgumentIndex + i].BoundName,
				argumentTypes[i]));
		}

		return [.. arguments];
	}

	// An argument that resolves to nothing is passed as its type's default rather than skipped: dropping it would
	// shift every argument after it onto the wrong parameter, which is a far worse failure than a zero. The rows are
	// required inputs precisely so this stays the unreachable case rather than what an unbound row means.
	private static Variant ResolveArgument(GraphContext graphContext, StringKey boundName, InteropValueType valueType)
	{
		if (InteropValues.IsObjectLane(valueType))
		{
			return graphContext.TryResolveObject(boundName, out Node? node)
				? InteropValues.ObjectToGodot(node)
				: default;
		}

		return graphContext.TryResolveVariant(boundName, out Variant128 value)
			? InteropValues.ToGodot(value, valueType)
			: InteropValues.ToGodot(default, valueType);
	}
}
