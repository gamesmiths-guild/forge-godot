// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Registry of object-backed Statescript variable types. All non-abstract <see cref="StatescriptObjectVariableType"/>
/// subclasses in the project assembly are discovered automatically, including ones defined in game code, so new object
/// variable types can be added without modifying the plugin or editor.
/// </summary>
public static class StatescriptObjectVariableTypeRegistry
{
	private static readonly List<StatescriptObjectVariableType> _all = [];
	private static readonly Dictionary<string, StatescriptObjectVariableType> _byTypeId = [];
	private static readonly Dictionary<Type, StatescriptObjectVariableType> _byClrType = [];

	/// <summary>
	/// Gets all registered object variable types.
	/// </summary>
	public static IReadOnlyList<StatescriptObjectVariableType> All => _all;

	static StatescriptObjectVariableTypeRegistry()
	{
		foreach (Type type in GetLoadableTypes(Assembly.GetExecutingAssembly()))
		{
			if (type.IsAbstract
				|| !typeof(StatescriptObjectVariableType).IsAssignableFrom(type)
				|| type.GetConstructor(Type.EmptyTypes) is null)
			{
				continue;
			}

			var descriptor = (StatescriptObjectVariableType)Activator.CreateInstance(type)!;
			_all.Add(descriptor);
			_byTypeId[descriptor.TypeId] = descriptor;
			_byClrType[descriptor.ClrType] = descriptor;
		}
	}

	/// <summary>
	/// Tries to get an object variable type by its <see cref="StatescriptObjectVariableType.TypeId"/>.
	/// </summary>
	/// <param name="typeId">The type id to look up.</param>
	/// <param name="descriptor">The matching descriptor when found.</param>
	/// <returns><see langword="true"/> when a descriptor is registered for the id.</returns>
	public static bool TryGet(string typeId, out StatescriptObjectVariableType descriptor)
	{
		return _byTypeId.TryGetValue(typeId ?? string.Empty, out descriptor!);
	}

	/// <summary>
	/// Tries to get an object variable type by its runtime CLR type.
	/// </summary>
	/// <param name="clrType">The CLR type to look up.</param>
	/// <param name="descriptor">The matching descriptor when found.</param>
	/// <returns><see langword="true"/> when a descriptor is registered for the CLR type.</returns>
	public static bool TryGetByClrType(Type clrType, out StatescriptObjectVariableType descriptor)
	{
		return _byClrType.TryGetValue(clrType, out descriptor!);
	}

	/// <summary>
	/// Gets a value indicating whether the given CLR type is a registered object variable type.
	/// </summary>
	/// <param name="clrType">The CLR type to check.</param>
	/// <returns><see langword="true"/> when the CLR type is registered.</returns>
	public static bool IsObjectType(Type clrType)
	{
		return _byClrType.ContainsKey(clrType);
	}

	private static Type[] GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return [.. ex.Types.Where(type => type is not null)!];
		}
	}
}
