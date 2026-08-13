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
	/// <returns>An array with the available attributes.</returns>
	public static string[] GetAttributeSetOptions()
	{
		var options = new List<string>();

		foreach (Type attributeSetType in AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(x => x.IsSubclassOf(typeof(AttributeSet))))
		{
			options.Add(attributeSetType.Name);
		}

		return [.. options];
	}

	/// <summary>
	/// Instantiates the given attribute set and gathers the names actually registered into its
	/// <see cref="AttributeSet.AttributesMap"/>.
	/// </summary>
	/// <remarks>
	/// The registered name is whatever string was passed to <c>InitializeAttribute</c>, which may differ from the
	/// property name. Reading the live keys keeps the editor in sync with attributes named freely by the user.
	/// <para>
	/// Those keys are <see cref="Forge.Core.StringKey"/>s, which normalize to lowercase, so they cannot be shown to the
	/// user as-is. Attributes are conventionally registered with <c>nameof</c> against a declared
	/// <see cref="EntityAttribute"/> member, so the declared member name is used to restore the original casing
	/// whenever one matches. Attributes named freely, with no member to match, keep the registered key.
	/// </para>
	/// </remarks>
	/// <param name="attributeSet">The attribute set used to search for the attributes.</param>
	/// <returns>An array with the available attributes.</returns>
	public static string[] GetAttributeOptions(string? attributeSet)
	{
		if (string.IsNullOrEmpty(attributeSet))
		{
			return [];
		}

		Type? type = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.FirstOrDefault(x => x.IsSubclassOf(typeof(AttributeSet)) && x.Name == attributeSet);

		if (type is null)
		{
			return [];
		}

		AttributeSet? instance = TryInstantiate(type);
		if (instance is null)
		{
			return [];
		}

		string prefix = $"{type.Name}.";
		Dictionary<string, string> declaredNames = GetDeclaredAttributeNames(type);

		return
		[
			.. instance.AttributesMap.Keys
				.Select(key => key.ToString())
				.Select(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
					? key[prefix.Length..]
					: key)
				.Select(key => declaredNames.TryGetValue(key, out string? declared) ? declared : key),
		];
	}

	/// <summary>
	/// Maps the lowercased name of every <see cref="EntityAttribute"/> member declared on the given attribute set to
	/// the name as it was written in code, so registered keys can be displayed with their original casing.
	/// </summary>
	/// <param name="type">The attribute set type to reflect over.</param>
	/// <returns>A lookup from lowercased member name to declared member name.</returns>
	private static Dictionary<string, string> GetDeclaredAttributeNames(Type type)
	{
		var names = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (PropertyInfo property in type.GetProperties(
			BindingFlags.Public | BindingFlags.Instance))
		{
			if (property.PropertyType == typeof(EntityAttribute))
			{
				names[property.Name.ToLowerInvariant()] = property.Name;
			}
		}

		foreach (FieldInfo field in type.GetFields(
			BindingFlags.Public | BindingFlags.Instance))
		{
			if (field.FieldType == typeof(EntityAttribute))
			{
				names[field.Name.ToLowerInvariant()] = field.Name;
			}
		}

		return names;
	}

	private static AttributeSet? TryInstantiate(Type type)
	{
		try
		{
			return Activator.CreateInstance(type) as AttributeSet;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
#endif
