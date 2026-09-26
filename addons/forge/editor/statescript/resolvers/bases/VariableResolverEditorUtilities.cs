// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class VariableResolverEditorUtilities
{
	private static List<string>? _cachedSharedVariableSetPaths;

	/// <summary>
	/// Gets the project paths of every <see cref="ForgeSharedVariableSet"/> asset. Results are cached until
	/// <see cref="InvalidateCache"/> is called, since every Variable resolver lists them as it is built.
	/// </summary>
	/// <returns>The shared variable set asset paths.</returns>
	public static IReadOnlyList<string> FindAllSharedVariableSetPaths()
	{
		// Matched on each file's header rather than by loading it: loading every resource in the project here crashed
		// the editor whenever this ran during layout restore, while the C# script instances were still being bound.
		_cachedSharedVariableSetPaths ??=
			ProjectFileIndex.CollectResourcesByScriptClass(nameof(ForgeSharedVariableSet));
		return _cachedSharedVariableSetPaths;
	}

	/// <summary>
	/// Clears the cached asset scan, so a set added, removed, or moved on disk is picked up.
	/// </summary>
	public static void InvalidateCache()
	{
		_cachedSharedVariableSetPaths = null;
	}

	public static string GetResourceDisplayName(string path)
	{
		string displayName = path[(path.LastIndexOf('/') + 1)..];
		if (displayName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
			|| displayName.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
		{
			int extensionIndex = displayName.LastIndexOf('.');
			displayName = displayName[..extensionIndex];
		}

		return displayName;
	}
}
#endif
