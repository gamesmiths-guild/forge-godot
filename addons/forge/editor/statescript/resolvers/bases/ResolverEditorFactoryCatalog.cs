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
		return GetCompatibleFactories(iterationScope: false, expectedTypes);
	}

	public static List<Func<NodeEditorProperty>> GetCompatibleFactories(
		bool iterationScope,
		params Type[] expectedTypes)
	{
		var result = new ResolverEditorFactoryList { IterationScope = iterationScope };
		var seenTypeIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (Type expectedType in expectedTypes)
		{
			result.AddRange(StatescriptResolverRegistry.GetCompatibleFactories(expectedType)
				.Where(factory => StatescriptResolverRegistry.SupportsScalarValues(factory)
					&& (iterationScope || !StatescriptResolverRegistry.RequiresIterationScope(factory))
					&& seenTypeIds.Add(StatescriptResolverRegistry.GetResolverTypeId(factory))));
		}

		return result;
	}

	public static List<Func<NodeEditorProperty>> GetArrayCompatibleFactories(params Type[] expectedTypes)
	{
		return GetArrayCompatibleFactories(iterationScope: false, expectedTypes);
	}

	public static List<Func<NodeEditorProperty>> GetArrayCompatibleFactories(
		bool iterationScope,
		params Type[] expectedTypes)
	{
		var result = new ResolverEditorFactoryList { IterationScope = iterationScope };
		var seenTypeIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (Type expectedType in expectedTypes)
		{
			result.AddRange(StatescriptResolverRegistry.GetCompatibleFactories(expectedType)
				.Where(factory => StatescriptResolverRegistry.SupportsArrayValues(factory)
					&& (iterationScope || !StatescriptResolverRegistry.RequiresIterationScope(factory))
					&& seenTypeIds.Add(StatescriptResolverRegistry.GetResolverTypeId(factory))));
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
