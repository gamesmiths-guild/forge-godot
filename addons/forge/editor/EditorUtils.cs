// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Resources.Attributes;

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

		// A set defined as a resource does not exist as a type until the project is rebuilt. Offering it anyway lets a
		// designer define a set and reference it straight away, instead of having to build before the set can be used.
		// Only definitions that will actually generate are offered: a definition that fails validation never becomes a
		// class, so selecting it would create a reference that can never resolve.
		HashSet<string> codeDefinedSets = AttributeSetCodeGenerator.GetCodeDefinedSetNames();

		options.AddRange(AttributeSetCodeGenerator.LoadDefinitions()
			.Where(definition => AttributeSetCodeGenerator.Validate(definition, null, codeDefinedSets).Length == 0)
			.Select(definition => definition.AttributeSetName)
			.Where(name => !options.Contains(name)));

		return [.. options];
	}

	/// <summary>
	/// Gathers only the attribute sets that exist as compiled types.
	/// </summary>
	/// <remarks>
	/// Used where the name has to resolve to a real type right now rather than eventually — picking the class a
	/// <c>ForgeAttributeSet</c> node instantiates, for instance, which reads the type's attributes to seed its initial
	/// values and would otherwise store an empty set of them for a definition still awaiting a build.
	/// </remarks>
	/// <returns>An array with the compiled attribute set names.</returns>
	public static string[] GetCompiledAttributeSetOptions()
	{
		return
		[
			.. AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(x => x.IsSubclassOf(typeof(AttributeSet)))
				.Select(x => x.Name),
		];
	}

	/// <summary>
	/// Instantiates a compiled attribute set by name.
	/// </summary>
	/// <param name="attributeSet">The attribute set name.</param>
	/// <returns>A new instance, or null when no such type is compiled or it cannot be constructed.</returns>
	public static AttributeSet? CreateCompiledAttributeSet(string? attributeSet)
	{
		Type? type = FindCompiledSetType(attributeSet);

		return type is null ? null : TryInstantiate(type);
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
		string[] compiled = GetCompiledAttributeOptions(attributeSet);

		if (compiled.Length > 0)
		{
			return compiled;
		}

		// Nothing compiled under that name, so fall back to a definition still waiting on a build. Its attributes are
		// already known, which keeps the second dropdown usable in the meantime.
		foreach (ForgeAttributeSetDefinition definition in AttributeSetCodeGenerator.LoadDefinitions())
		{
			if (definition.AttributeSetName == attributeSet)
			{
				return
				[
					.. definition.Attributes
						.Where(attribute => attribute?.AttributeName.Length > 0)
						.Select(attribute => attribute.AttributeName),
				];
			}
		}

		return [];
	}

	/// <summary>
	/// Gathers the attributes of a set that exists as a compiled type, ignoring definitions that are still waiting on a
	/// build. This is what tells a definition apart from the class generated for it.
	/// </summary>
	/// <param name="attributeSet">The attribute set used to search for the attributes.</param>
	/// <returns>An array with the available attributes, empty when the set is not compiled.</returns>
	public static string[] GetCompiledAttributeOptions(string? attributeSet)
	{
		Type? type = FindCompiledSetType(attributeSet);

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

	private static Type? FindCompiledSetType(string? attributeSet)
	{
		if (string.IsNullOrEmpty(attributeSet))
		{
			return null;
		}

		return AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.FirstOrDefault(x => x.IsSubclassOf(typeof(AttributeSet)) && x.Name == attributeSet);
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
