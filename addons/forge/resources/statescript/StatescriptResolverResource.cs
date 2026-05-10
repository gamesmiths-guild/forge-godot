// Copyright © Gamesmiths Guild.

using System;
using System.Linq;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Base resource for all Statescript property resolvers. Each resolver type derives from this and implements the
/// binding methods to wire serialized editor data into the core runtime graph.
/// </summary>
/// <remarks>
/// Subclasses must override <see cref="ResolverTypeId"/> to return a unique string that matches the corresponding
/// editor's <c>ResolverTypeId</c>. This enables automatic discovery and matching without hardcoded registrations.
/// </remarks>
[Tool]
[GlobalClass]
public partial class StatescriptResolverResource : Resource
{
	/// <summary>
	/// Gets the unique type identifier for this resolver, used to match serialized resources to their corresponding
	/// editor. Subclasses must override this to return a non-empty string that matches their editor's
	/// <c>ResolverTypeId</c>.
	/// </summary>
	public virtual string ResolverTypeId => string.Empty;

	/// <summary>
	/// Binds this resolver as an input property on a runtime node. Implementations should register any necessary
	/// variable or property definitions on the graph and call <see cref="ForgeNode.BindInput"/> with the appropriate
	/// name.
	/// </summary>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="runtimeNode">The runtime node to bind the input on.</param>
	/// <param name="nodeId">The serialized node identifier, used for generating unique property names.</param>
	/// <param name="index">The zero-based index of the input property to bind.</param>
	public virtual void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
	}

	/// <summary>
	/// Binds this resolver as an output variable on a runtime node. Implementations should call
	/// <see cref="ForgeNode.BindOutput(byte, StringKey, VariableScope)"/> with the appropriate variable name.
	/// </summary>
	/// <param name="runtimeNode">The runtime node to bind the output on.</param>
	/// <param name="index">The zero-based index of the output variable to bind.</param>
	public virtual void BindOutput(ForgeNode runtimeNode, byte index)
	{
	}

	/// <summary>
	/// Creates an <see cref="IPropertyResolver"/> from this resolver resource. Used when this resolver appears as a
	/// nested operand inside another resolver (e.g., left/right side of a comparison).
	/// </summary>
	/// <param name="graph">The runtime graph being built, used for looking up variable type information.</param>
	/// <returns>The property resolver instance, or a default zero-value resolver if the resource is not configured.
	/// </returns>
	public virtual IPropertyResolver BuildResolver(Graph graph)
	{
		return new VariantResolver(default, typeof(int));
	}

	protected static void DefineAndBindInputProperty(
		Graph graph,
		ForgeNode runtimeNode,
		string propertyName,
		byte index,
		IPropertyResolver resolver)
	{
		var propertyKey = new StringKey(propertyName);
		graph.VariableDefinitions.DefineProperty(
			propertyKey,
			AdaptResolverForExpectedInput(runtimeNode, index, resolver));
		runtimeNode.BindInput(index, propertyKey);
	}

	protected static bool NeedsNumericInputAdaptation(
		ForgeNode runtimeNode,
		byte index,
		Type actualType)
	{
		if (index >= runtimeNode.InputProperties.Length)
		{
			return false;
		}

		Type expectedType = runtimeNode.InputProperties[index].ExpectedType;
		return expectedType != typeof(Variant128)
			&& expectedType != actualType
			&& StatescriptNumericCompatibility.CanCoerce(actualType, expectedType);
	}

	protected static IPropertyResolver AdaptResolverForExpectedType(
		IPropertyResolver resolver,
		Type expectedType)
	{
		if (expectedType == typeof(Variant128) || expectedType == resolver.ValueType)
		{
			return resolver;
		}

		if (StatescriptNumericCompatibility.CanCoerce(resolver.ValueType, expectedType))
		{
			return new NumericCoercionResolver(resolver, expectedType);
		}

		return resolver;
	}

	protected static IPropertyResolver PromoteIntegralResolverToFloatingPoint(
		IPropertyResolver resolver,
		Type preferredFloatingType)
	{
		if (!StatescriptNumericCompatibility.IsNumericType(resolver.ValueType)
			|| IsFloatingPointType(resolver.ValueType))
		{
			return resolver;
		}

		return AdaptResolverForExpectedType(resolver, preferredFloatingType);
	}

	protected static Type GetPreferredFloatingPointType(params IPropertyResolver[] resolvers)
	{
		foreach (Type resolverType in resolvers.Select(resolver => resolver.ValueType))
		{
			if (resolverType == typeof(double) || resolverType == typeof(decimal))
			{
				return typeof(double);
			}
		}

		foreach (IPropertyResolver resolver in resolvers)
		{
			if (resolver.ValueType == typeof(float))
			{
				return typeof(float);
			}
		}

		return typeof(double);
	}

	protected static bool IsVectorOrQuaternionType(Type type)
	{
		return type == typeof(System.Numerics.Vector2)
			|| type == typeof(System.Numerics.Vector3)
			|| type == typeof(System.Numerics.Vector4)
			|| type == typeof(System.Numerics.Quaternion);
	}

	private static IPropertyResolver AdaptResolverForExpectedInput(
		ForgeNode runtimeNode,
		byte index,
		IPropertyResolver resolver)
	{
		if (index >= runtimeNode.InputProperties.Length)
		{
			return resolver;
		}

		Type expectedType = runtimeNode.InputProperties[index].ExpectedType;
		return AdaptResolverForExpectedType(resolver, expectedType);
	}

	private static bool IsFloatingPointType(Type type)
	{
		return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
	}
}
