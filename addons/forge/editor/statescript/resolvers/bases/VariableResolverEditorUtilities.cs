// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class VariableResolverEditorUtilities
{
	public static List<string> FindAllSharedVariableSetPaths()
	{
		var results = new List<string>();
		EditorFileSystemDirectory root = EditorInterface.Singleton.GetResourceFilesystem().GetFilesystem();
		ScanFilesystemDirectory(root, results);
		return results;
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

	private static void ScanFilesystemDirectory(EditorFileSystemDirectory dir, List<string> results)
	{
		for (int i = 0; i < dir.GetFileCount(); i++)
		{
			string path = dir.GetFilePath(i);
			if (!path.EndsWith(".tres", StringComparison.InvariantCultureIgnoreCase)
				&& !path.EndsWith(".res", StringComparison.InvariantCultureIgnoreCase))
			{
				continue;
			}

			// The recorded script class comes from the resource header, so the type is known without opening the
			// file. Loading every resource in the project here crashed the editor whenever this ran during layout
			// restore, while the C# script instances were still being bound.
			if (dir.GetFileScriptClassName(i) != nameof(ForgeSharedVariableSet)
				&& dir.GetFileType(i) != nameof(ForgeSharedVariableSet))
			{
				continue;
			}

			results.Add(path);
		}

		for (int i = 0; i < dir.GetSubdirCount(); i++)
		{
			ScanFilesystemDirectory(dir.GetSubdir(i), results);
		}
	}
}
#endif
