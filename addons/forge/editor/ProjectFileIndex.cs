// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Lists project files straight from the editor's filesystem index.
/// </summary>
/// <remarks>
/// The editor already indexes every file and keeps it in memory, so walking it costs nothing and, unlike a
/// <see cref="DirAccess"/> recursion, cannot pick up files the editor is deliberately ignoring or hand back the
/// malformed <c>res:///</c> paths that manual path joining produces.
/// </remarks>
internal static class ProjectFileIndex
{
	/// <summary>
	/// Collects every indexed file of a given resource type.
	/// </summary>
	/// <param name="resourceType">The base resource type, such as <c>"Resource"</c> or <c>"PackedScene"</c>.</param>
	/// <param name="extensions">The file extensions to accept, including the leading dot.</param>
	/// <returns>The matching resource paths.</returns>
	public static List<string> CollectByType(string resourceType, params string[] extensions)
	{
		var paths = new List<string>();
		EditorFileSystemDirectory? root = EditorInterface.Singleton.GetResourceFilesystem().GetFilesystem();

		if (root is not null)
		{
			CollectRecursive(root, resourceType, extensions, paths);
		}

		return paths;
	}

	private static void CollectRecursive(
		EditorFileSystemDirectory directory,
		string resourceType,
		string[] extensions,
		List<string> paths)
	{
		for (int i = 0; i < directory.GetFileCount(); i++)
		{
			if (directory.GetFileType(i) != resourceType)
			{
				continue;
			}

			string path = directory.GetFilePath(i);

			if (Array.Exists(extensions, extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
			{
				paths.Add(path);
			}
		}

		for (int i = 0; i < directory.GetSubdirCount(); i++)
		{
			CollectRecursive(directory.GetSubdir(i), resourceType, extensions, paths);
		}
	}
}
#endif
