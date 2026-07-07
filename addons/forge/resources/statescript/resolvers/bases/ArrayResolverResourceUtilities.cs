// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Shared helpers for array-operation resolver resources: nested source resolution and closing the object-lane generic
/// resolver types over a runtime element type.
/// </summary>
internal static class ArrayResolverResourceUtilities
{
	internal static bool TryResolveSource(
		StatescriptResolverResource? source,
		string resolverTypeId,
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (source is null)
		{
			GD.PushError($"Statescript: {resolverTypeId} resolver is missing an array source.");
			return false;
		}

		if (!source.TryBuildArrayResolver(graph, out valueArrayResolver, out objectArrayResolver))
		{
			GD.PushError(
				$"Statescript: {resolverTypeId} resolver source '{source.ResolverTypeId}' does not produce an " +
				"array.");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Composes an object-backed array (<c>ObjectArrayCompositeResolver&lt;T&gt;</c>) from per-element object
	/// resolvers, closed over <paramref name="elementType"/>. Each element resolver must resolve to a value assignable
	/// to <paramref name="elementType"/>.
	/// </summary>
	/// <param name="elementType">The array's element type.</param>
	/// <param name="elements">The per-element object resolvers, in order.</param>
	/// <returns>The composed object array resolver, or <see langword="null"/> when an element is incompatible.
	/// </returns>
	internal static IObjectArrayResolver? CreateObjectArrayComposite(
		Type elementType,
		IReadOnlyList<IObjectResolver> elements)
	{
		Type resolverInterface = typeof(IObjectResolver<>).MakeGenericType(elementType);
		var typedElements = Array.CreateInstance(resolverInterface, elements.Count);

		for (int i = 0; i < elements.Count; i++)
		{
			if (!resolverInterface.IsInstanceOfType(elements[i]))
			{
				GD.PushError(
					$"Statescript: array element {i} resolves to '{elements[i].ValueType.Name}', which is not " +
					$"assignable to the array element type '{elementType.Name}'.");
				return null;
			}

			typedElements.SetValue(elements[i], i);
		}

		Type compositeType = typeof(ObjectArrayCompositeResolver<>).MakeGenericType(elementType);
		return (IObjectArrayResolver)Activator.CreateInstance(compositeType, [typedElements])!;
	}

	/// <summary>
	/// Instantiates an <c>ObjectAppendResolver&lt;T&gt;</c> closed over the source's element type, appending the given
	/// per-element object resolvers. Each element resolver must resolve to a value assignable to the element type.
	/// </summary>
	/// <param name="source">The object-lane source array resolver.</param>
	/// <param name="elements">The per-element object resolvers to append, in order.</param>
	/// <returns>The composed append resolver, or <see langword="null"/> when an element is incompatible.</returns>
	internal static IObjectArrayResolver? CreateObjectAppend(
		IObjectArrayResolver source,
		IReadOnlyList<IObjectResolver> elements)
	{
		Type elementType = source.ElementType;
		Type resolverInterface = typeof(IObjectResolver<>).MakeGenericType(elementType);
		var typedElements = Array.CreateInstance(resolverInterface, elements.Count);

		for (int i = 0; i < elements.Count; i++)
		{
			if (!resolverInterface.IsInstanceOfType(elements[i]))
			{
				GD.PushError(
					$"Statescript: Append resolver element {i} resolves to '{elements[i].ValueType.Name}', which is " +
					$"not assignable to the array element type '{elementType.Name}'. Skipping the append.");
				return null;
			}

			typedElements.SetValue(elements[i], i);
		}

		Type resolverType = typeof(ObjectAppendResolver<>).MakeGenericType(elementType);
		return (IObjectArrayResolver)Activator.CreateInstance(resolverType, [source, typedElements])!;
	}

	/// <summary>
	/// Instantiates an object-lane array operation (e.g. <c>ObjectWhereResolver&lt;T&gt;</c>) closed over the source's
	/// element type. The source is always passed as the first constructor argument.
	/// </summary>
	/// <param name="openGenericType">The open generic resolver type (e.g. <c>typeof(ObjectWhereResolver&lt;&gt;)</c>).
	/// </param>
	/// <param name="source">The object-lane source array resolver.</param>
	/// <param name="additionalArguments">The remaining constructor arguments, in order.</param>
	/// <returns>The closed operation resolver.</returns>
	internal static IObjectArrayResolver CreateObjectArrayOperation(
		Type openGenericType,
		IObjectArrayResolver source,
		params object?[] additionalArguments)
	{
		Type closedType = openGenericType.MakeGenericType(source.ElementType);
		object?[] arguments = new object?[additionalArguments.Length + 1];
		arguments[0] = source;
		Array.Copy(additionalArguments, 0, arguments, 1, additionalArguments.Length);
		return (IObjectArrayResolver)Activator.CreateInstance(closedType, arguments)!;
	}

	/// <summary>
	/// Instantiates an object-lane array access resolver (e.g. <c>ObjectFirstResolver&lt;T&gt;</c>) closed over the
	/// source's element type. The source is always passed as the first constructor argument. Unlike
	/// <see cref="CreateObjectArrayOperation"/> the result is a scalar object resolver rather than an array.
	/// </summary>
	/// <param name="openGenericType">The open generic resolver type (e.g. <c>typeof(ObjectFirstResolver&lt;&gt;)</c>).
	/// </param>
	/// <param name="source">The object-lane source array resolver.</param>
	/// <param name="additionalArguments">The remaining constructor arguments, in order.</param>
	/// <returns>The closed access resolver.</returns>
	internal static IObjectResolver CreateObjectAccessResolver(
		Type openGenericType,
		IObjectArrayResolver source,
		params object?[] additionalArguments)
	{
		Type closedType = openGenericType.MakeGenericType(source.ElementType);
		object?[] arguments = new object?[additionalArguments.Length + 1];
		arguments[0] = source;
		Array.Copy(additionalArguments, 0, arguments, 1, additionalArguments.Length);
		return (IObjectResolver)Activator.CreateInstance(closedType, arguments)!;
	}

	/// <summary>
	/// Resolves a nested source that must produce an object-backed (reference) array of any registered object type,
	/// reporting an editor error otherwise.
	/// </summary>
	/// <param name="source">The nested source resource, may be <see langword="null"/>.</param>
	/// <param name="resolverTypeId">The owning resolver's type id (for error messages).</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="objectArrayResolver">The object array resolver when the source produces one.</param>
	/// <returns><see langword="true"/> when the source produced an object array.</returns>
	internal static bool TryResolveObjectArraySource(
		StatescriptResolverResource? source,
		string resolverTypeId,
		Graph graph,
		out IObjectArrayResolver? objectArrayResolver)
	{
		if (!TryResolveSource(source, resolverTypeId, graph, out _, out objectArrayResolver))
		{
			return false;
		}

		if (objectArrayResolver is null)
		{
			GD.PushError($"Statescript: {resolverTypeId} resolver requires an object array source.");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Resolves a nested source that must produce an entity array, reporting an editor error otherwise.
	/// </summary>
	/// <param name="source">The nested source resource, may be <see langword="null"/>.</param>
	/// <param name="resolverTypeId">The owning resolver's type id (for error messages).</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="entityArrayResolver">The entity array resolver when the source produces one.</param>
	/// <returns><see langword="true"/> when the source produced an entity array.</returns>
	internal static bool TryResolveEntityArraySource(
		StatescriptResolverResource? source,
		string resolverTypeId,
		Graph graph,
		out IObjectArrayResolver<IForgeEntity>? entityArrayResolver)
	{
		entityArrayResolver = null;

		if (!TryResolveSource(source, resolverTypeId, graph, out _, out IObjectArrayResolver? objectArrayResolver))
		{
			return false;
		}

		if (objectArrayResolver is IObjectArrayResolver<IForgeEntity> typedResolver)
		{
			entityArrayResolver = typedResolver;
			return true;
		}

		GD.PushError($"Statescript: {resolverTypeId} resolver requires an entity array source.");
		return false;
	}

	/// <summary>
	/// Resolves a nested source that must produce a numeric value-typed array, reporting an editor error otherwise.
	/// </summary>
	/// <param name="source">The nested source resource, may be <see langword="null"/>.</param>
	/// <param name="resolverTypeId">The owning resolver's type id (for error messages).</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="valueArrayResolver">The numeric array resolver when the source produces one.</param>
	/// <returns><see langword="true"/> when the source produced a numeric array.</returns>
	internal static bool TryResolveNumericArraySource(
		StatescriptResolverResource? source,
		string resolverTypeId,
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver)
	{
		if (!TryResolveSource(source, resolverTypeId, graph, out valueArrayResolver, out _))
		{
			return false;
		}

		if (valueArrayResolver is null || !IsNumericValueType(valueArrayResolver.ElementType))
		{
			GD.PushError($"Statescript: {resolverTypeId} resolver requires a numeric array source.");
			valueArrayResolver = null;
			return false;
		}

		return true;
	}

	/// <summary>
	/// Builds an optional boolean predicate from a nested resource. Returns <see langword="null"/> when the resource
	/// is missing, reporting an editor error (and returning <see langword="null"/>) when it does not resolve to
	/// <see langword="bool"/>.
	/// </summary>
	/// <param name="predicate">The nested predicate resource, may be <see langword="null"/>.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="resolverTypeId">The owning resolver's type id (for error messages).</param>
	/// <returns>The predicate resolver, or <see langword="null"/>.</returns>
	internal static IPropertyResolver? BuildOptionalPredicateResolver(
		StatescriptResolverResource? predicate,
		Graph graph,
		string resolverTypeId)
	{
		if (predicate is null)
		{
			return null;
		}

		IPropertyResolver resolver = predicate.BuildResolver(graph);

		if (resolver.ValueType != typeof(bool))
		{
			GD.PushError(
				$"Statescript: {resolverTypeId} resolver predicate must resolve to bool. Got " +
				$"'{resolver.ValueType}'. Ignoring it.");
			return null;
		}

		return resolver;
	}

	/// <summary>
	/// Checks whether a resolver value type is numeric (usable as a sort key, count, or index).
	/// </summary>
	/// <param name="type">The value type to check.</param>
	/// <returns><see langword="true"/> when the type is numeric.</returns>
	internal static bool IsNumericValueType(Type type)
	{
		return type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong)
			|| type == typeof(float)
			|| type == typeof(double)
			|| type == typeof(decimal);
	}

	/// <summary>
	/// Builds a numeric operand (count or index) from a nested resource, reporting an editor error and falling back to
	/// a constant when the operand is missing or not numeric.
	/// </summary>
	/// <param name="operand">The nested operand resource, may be <see langword="null"/>.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="resolverTypeId">The owning resolver's type id (for error messages).</param>
	/// <param name="operandName">The operand's designer-facing name (for error messages).</param>
	/// <param name="fallbackValue">The constant used when the operand is missing or invalid.</param>
	/// <returns>The numeric operand resolver.</returns>
	internal static IPropertyResolver BuildNumericOperandResolver(
		StatescriptResolverResource? operand,
		Graph graph,
		string resolverTypeId,
		string operandName,
		int fallbackValue)
	{
		if (operand is null)
		{
			GD.PushError(
				$"Statescript: {resolverTypeId} resolver is missing its {operandName}; using {fallbackValue}.");
			return new VariantResolver(new Variant128(fallbackValue), typeof(int));
		}

		IPropertyResolver resolver = operand.BuildResolver(graph);

		if (!IsNumericValueType(resolver.ValueType))
		{
			GD.PushError(
				$"Statescript: {resolverTypeId} resolver {operandName} must resolve to a numeric type. Got " +
				$"'{resolver.ValueType}'. Using {fallbackValue}.");
			return new VariantResolver(new Variant128(fallbackValue), typeof(int));
		}

		return resolver;
	}
}
