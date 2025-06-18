// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Godot.Editor;

internal static class EditorUtils
{
	/// <summary>
	/// Uses reflection to gather all classes inheriting from AttributeSet and their fields of type Attribute.
	/// </summary>
	/// <returns>An array with the avaiable attributes.</returns>
	public static string[] GetAttributeSetOptions()
	{
		var options = new List<string>();

		// Get all types in the current assembly
		Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		// Find all types that subclass AttributeSet
		foreach (Type attributeSetType in allTypes.Where(x => x.IsSubclassOf(typeof(AttributeSet))))
		{
			options.Add(attributeSetType.Name);
		}

		return [.. options];
	}

	/// <summary>
	/// Uses reflection to gather all classes inheriting from AttributeSet and their fields of type Attribute.
	/// </summary>
	/// <param name="attributeSet">The attribute set used to search for the attributes.</param>
	/// <returns>An array with the avaiable attributes.</returns>
	public static string[] GetAttributeOptions(string? attributeSet)
	{
		if (string.IsNullOrEmpty(attributeSet))
		{
			return [];
		}

		var asm = Assembly.GetExecutingAssembly();
		Type? type = Array.Find(
			asm.GetTypes(),
			x => x.IsSubclassOf(typeof(AttributeSet)) && x.Name == attributeSet);

		if (type is null)
		{
			return [];
		}

		IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.PropertyType == typeof(EntityAttribute));

		return [.. properties.Select(x => $"{x.Name}")];
	}
}
#endif
