// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class VariableResolverEditorUtilities
{
	public static List<string> FindAllSharedVariableSetPaths()
	{
		// Matched on each file's header rather than by loading it: loading every resource in the project here crashed
		// the editor whenever this ran during layout restore, while the C# script instances were still being bound.
		return ProjectFileIndex.CollectResourcesByScriptClass(nameof(ForgeSharedVariableSet));
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
