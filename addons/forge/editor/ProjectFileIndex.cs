// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
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

	/// <summary>
	/// Collects every text resource whose script is a given <c>[GlobalClass]</c>.
	/// </summary>
	/// <remarks>
	/// Godot records the class in the <c>[gd_resource]</c> header, so only the first line of each candidate is read.
	/// The editor knows this too, but keeps it in a field with no script binding: <c>get_file_script_class_name</c>
	/// exposes the class a <em>script file</em> declares, which is empty for a resource, and the resource's own
	/// <c>resource_script_class</c> is not bound at all. Reading the header avoids loading every resource in the
	/// project, which is what a list rebuilt as often as the attribute pickers cannot afford.
	/// </remarks>
	/// <param name="scriptClassName">The <c>[GlobalClass]</c> name to match.</param>
	/// <returns>The matching resource paths.</returns>
	public static List<string> CollectResourcesByScriptClass(string scriptClassName)
	{
		string marker = $"script_class=\"{scriptClassName}\"";

		// Binary resources keep no readable header, so only the text format is searched. Definitions are authored
		// through the inspector, which writes .tres.
		return [.. CollectByType("Resource", ".tres").Where(path => HeaderContains(path, marker))];
	}

	private static bool HeaderContains(string path, string marker)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

		return file?.GetLine().Contains(marker, StringComparison.Ordinal) == true;
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
