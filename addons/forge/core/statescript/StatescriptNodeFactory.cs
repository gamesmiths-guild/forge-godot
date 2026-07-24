// Copyright © Gamesmiths Guild.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Core;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using GodotDictionary = Godot.Collections.Dictionary<string, Godot.Variant>;
using GodotVariant = Godot.Variant;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Instantiates runtime Statescript nodes from a serialized runtime type name and the constructor arguments stored in a
/// node's <c>CustomData</c>.
/// </summary>
/// <remarks>
/// Both <see cref="StatescriptGraphBuilder"/> and the editor's node discovery create their instances through this
/// factory, so the ports the editor draws are always the ports the built graph has. Constructor parameters are matched
/// by name against <c>CustomData</c>; unmatched parameters fall back to their declared default value.
/// </remarks>
public static class StatescriptNodeFactory
{
	/// <summary>
	/// Resolves a node type from its fully qualified name, scanning all loaded assemblies.
	/// </summary>
	/// <param name="typeName">The fully qualified type name stored in the node resource.</param>
	/// <returns>The resolved type, or <see langword="null"/> when no loaded assembly declares it.</returns>
	public static Type? ResolveType(string typeName)
	{
		var type = Type.GetType(typeName);
		if (type is not null)
		{
			return type;
		}

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			type = assembly.GetType(typeName);
			if (type is not null)
			{
				return type;
			}
		}

		return null;
	}

	/// <summary>
	/// Creates a node instance of the given type, feeding its constructor from the given custom data.
	/// </summary>
	/// <param name="nodeType">The concrete node type to instantiate.</param>
	/// <param name="customData">The node's custom data, keyed by constructor parameter name. May be
	/// <see langword="null"/> to build the node from its constructor defaults alone.</param>
	/// <returns>The instantiated node.</returns>
	public static ForgeNode Create(Type nodeType, GodotDictionary? customData)
	{
		ConstructorInfo? constructor = GetPrimaryConstructor(nodeType);

		if (constructor is null)
		{
			return (ForgeNode)Activator.CreateInstance(nodeType)!;
		}

		ParameterInfo[] parameters = constructor.GetParameters();
		object[] args = new object[parameters.Length];

		for (int i = 0; i < parameters.Length; i++)
		{
			ParameterInfo parameter = parameters[i];
			string parameterName = parameter.Name ?? string.Empty;

			args[i] = customData is not null && customData.TryGetValue(parameterName, out GodotVariant value)
				? ConvertParameter(value, parameter.ParameterType)
				: GetParameterDefaultValue(parameter);
		}

		return (ForgeNode)constructor.Invoke(args);
	}

	/// <summary>
	/// Gets the parameter names of the constructor used by <see cref="Create"/>.
	/// </summary>
	/// <param name="nodeType">The concrete node type.</param>
	/// <returns>The constructor parameter names, empty when the type has no public constructor.</returns>
	public static string[] GetConstructorParameterNames(Type nodeType)
	{
		ConstructorInfo? constructor = GetPrimaryConstructor(nodeType);

		return constructor is null
			? []
			: [.. constructor.GetParameters().Select(x => x.Name ?? string.Empty)];
	}

	private static ConstructorInfo? GetPrimaryConstructor(Type nodeType)
	{
		ConstructorInfo[] constructors = nodeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		return constructors.Length == 0
			? null
			: constructors.OrderByDescending(x => x.GetParameters().Length).First();
	}

	private static object ConvertParameter(GodotVariant value, Type targetType)
	{
		if (targetType.IsEnum)
		{
			if (value.VariantType == GodotVariant.Type.Int || value.VariantType == GodotVariant.Type.Float)
			{
				return Enum.ToObject(targetType, value.AsInt32());
			}

			string enumText = value.AsString();
			if (!string.IsNullOrEmpty(enumText))
			{
				return Enum.Parse(targetType, enumText, ignoreCase: true);
			}
		}

		if (targetType == typeof(StringKey))
		{
			return new StringKey(value.AsString());
		}

		if (targetType == typeof(string))
		{
			return value.AsString();
		}

		if (targetType == typeof(int))
		{
			return value.AsInt32();
		}

		if (targetType == typeof(float))
		{
			return value.AsSingle();
		}

		if (targetType == typeof(double))
		{
			return value.AsDouble();
		}

		if (targetType == typeof(bool))
		{
			return value.AsBool();
		}

		if (targetType == typeof(long))
		{
			return value.AsInt64();
		}

		return Convert.ChangeType(value.AsString(), targetType, CultureInfo.InvariantCulture);
	}

	private static object GetParameterDefaultValue(ParameterInfo parameter)
	{
		if (!parameter.HasDefaultValue || parameter.DefaultValue is DBNull)
		{
			return GetDefaultValue(parameter.ParameterType);
		}

		object? defaultValue = parameter.DefaultValue;

		if (defaultValue is null)
		{
			return null!;
		}

		Type parameterType = parameter.ParameterType;

		if (parameterType.IsEnum)
		{
			return defaultValue.GetType().IsEnum
				? defaultValue
				: Enum.ToObject(parameterType, defaultValue);
		}

		if (parameterType == typeof(StringKey) && defaultValue is string stringKeyValue)
		{
			return new StringKey(stringKeyValue);
		}

		if (parameterType.IsInstanceOfType(defaultValue))
		{
			return defaultValue;
		}

		return Convert.ChangeType(defaultValue, parameterType, CultureInfo.InvariantCulture);
	}

	private static object GetDefaultValue(Type type)
	{
		if (type == typeof(StringKey))
		{
			return new StringKey("_default_");
		}

		if (type == typeof(string))
		{
			return string.Empty;
		}

		if (type.IsValueType)
		{
			return Activator.CreateInstance(type)!;
		}

		return null!;
	}
}
