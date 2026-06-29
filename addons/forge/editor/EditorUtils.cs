// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
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

		if (type is null || Activator.CreateInstance(type) is not AttributeSet instance)
		{
			return [];
		}

		string prefix = $"{type.Name}.";
		return
		[
			.. instance.AttributesMap.Keys
				.Select(key => key.ToString())
				.Select(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
					? key[prefix.Length..]
					: key),
		];
	}
}
#endif
