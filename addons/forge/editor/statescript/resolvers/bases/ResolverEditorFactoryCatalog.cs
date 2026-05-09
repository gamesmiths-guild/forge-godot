// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
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
			result.AddRange(StatescriptResolverRegistry.GetCompatibleFactories(expectedType)
				.Where(factory => seenTypeIds.Add(StatescriptResolverRegistry.GetResolverTypeId(factory))));
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
			for (int i = 0; i < factories.Count; i++)
			{
				if (StatescriptResolverRegistry.GetResolverTypeId(factories[i]) == existingResolver.ResolverTypeId)
				{
					return i;
				}
			}
		}

		for (int i = 0; i < factories.Count; i++)
		{
			if (StatescriptResolverRegistry.GetResolverTypeId(factories[i]) == preferredResolverTypeId)
			{
				return i;
			}
		}

		return 0;
	}
}
#endif
