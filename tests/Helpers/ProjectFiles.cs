// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gamesmiths.Forge.Godot.Tests.Helpers;

/// <summary>
/// Locates the Godot project on disk and enumerates its assets as <c>res://</c> paths.
/// </summary>
/// <remarks>
/// Uses <see cref="System.IO"/> rather than <c>DirAccess</c> so that data sources can be enumerated during test
/// discovery, which happens outside the Godot runtime.
/// </remarks>
internal static class ProjectFiles
{
	private static readonly string[] _skippedDirectories = [".godot", ".git", "bin", "obj", "gdunit4_testadapter_v5"];

	/// <summary>
	/// Gets the absolute path of the directory holding <c>project.godot</c>.
	/// </summary>
	public static string ProjectRoot { get; } = FindProjectRoot();

	/// <summary>
	/// Enumerates every asset with one of the given extensions as a <c>res://</c> path.
	/// </summary>
	/// <param name="extensions">Extensions to match, including the leading dot.</param>
	/// <returns>The matching <c>res://</c> paths, ordered so test output is stable.</returns>
	public static IEnumerable<string> ResourcePaths(params string[] extensions)
	{
		return Walk(ProjectRoot)
			.Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
			.Select(path => "res://" + Path.GetRelativePath(ProjectRoot, path)
				.Replace(Path.DirectorySeparatorChar, '/'))
			.OrderBy(path => path, StringComparer.Ordinal);
	}

	private static IEnumerable<string> Walk(string directory)
	{
		// Pruning while descending rather than filtering afterwards: build output under .godot nests deep enough to
		// trip the Windows path limit, and a recursive EnumerateFiles throws on the first directory it cannot read.
		foreach (string file in Directory.EnumerateFiles(directory))
		{
			yield return file;
		}

		foreach (string subdirectory in Directory.EnumerateDirectories(directory))
		{
			if (_skippedDirectories.Contains(Path.GetFileName(subdirectory), StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			foreach (string file in Walk(subdirectory))
			{
				yield return file;
			}
		}
	}

	private static string FindProjectRoot()
	{
		// gdUnit4 runs tests with the Godot project as the working directory. The assembly location is the fallback
		// for any other host, where the build output sits under .godot/mono/temp/bin/<Configuration>/.
		string[] anchors =
		[
			Directory.GetCurrentDirectory(),
			Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
			AppContext.BaseDirectory,
		];

		foreach (string anchor in anchors.Where(anchor => !string.IsNullOrEmpty(anchor)))
		{
			for (DirectoryInfo? directory = new(anchor); directory is not null; directory = directory.Parent)
			{
				if (File.Exists(Path.Combine(directory.FullName, "project.godot")))
				{
					return directory.FullName;
				}
			}
		}

		throw new InvalidOperationException(
			$"Could not find project.godot above any of: {string.Join(", ", anchors)}");
	}
}
