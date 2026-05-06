// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class ResolverEditorFactoryCatalog
{
	public static List<Func<NodeEditorProperty>> GetCompatibleFactories(params Type[] expectedTypes)
	{
		var result = new List<Func<NodeEditorProperty>>();
		var seenTypeIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (Type expectedType in expectedTypes)
		{
			foreach (Func<NodeEditorProperty> factory in
				StatescriptResolverRegistry.GetCompatibleFactories(expectedType))
			{
				using NodeEditorProperty temp = factory();

				if (seenTypeIds.Add(temp.ResolverTypeId))
				{
					result.Add(factory);
				}
			}
		}

		return result;
	}

	public static int GetDefaultFactoryIndex(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver,
		string preferredResolverTypeId)
	{
		if (existingResolver is not null)
		{
			for (var i = 0; i < factories.Count; i++)
			{
				using NodeEditorProperty temp = factories[i]();

				if (temp.ResolverTypeId == existingResolver.ResolverTypeId)
				{
					return i;
				}
			}
		}

		for (var i = 0; i < factories.Count; i++)
		{
			using NodeEditorProperty temp = factories[i]();

			if (temp.ResolverTypeId == preferredResolverTypeId)
			{
				return i;
			}
		}

		return 0;
	}
}
#endif
