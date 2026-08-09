// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Resources;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// Finds resources in the project without loading them.
/// </summary>
/// <remarks>
/// <para>
/// Godot's editor filesystem already indexes every file, so collecting candidates costs nothing but a walk over data
/// that is in memory anyway. What it does not expose to scripts is the script class of a <c>.tres</c> - only the base
/// type, always <c>"Resource"</c> - so identifying a tag source needs a second step.
/// </para>
/// <para>
/// That step reads dependencies rather than the file's <c>script_class</c> header. The header is cheaper but is only
/// written for global classes, so it is absent from every file saved before a resource type became one - including
/// files a user has been carrying since an earlier version. Dependencies also catch a file whose script reference lost
/// its UID and fell back to a path.
/// </para>
/// <para>
/// Everything here runs only when the user asks for it. Nothing scans in the background, because a project with
/// thousands of resources would pay for it on every editor tick.
/// </para>
/// </remarks>
internal static class TagsSourceScanner
{
	// Used when the global class list is unavailable, which happens before the C# assembly has been built once.
	private const string TagsSourceScriptUid = "uid://bq4vlbfx00hea";

	/// <summary>
	/// Finds every tag source file in the project.
	/// </summary>
	/// <returns>The resource paths of the tag sources found, in filesystem order.</returns>
	public static List<string> FindTagSources()
	{
		ScriptIdentity identity = ResolveScriptIdentity(nameof(ForgeTagsSource), TagsSourceScriptUid);

		return [.. CollectResourceCandidates().Where(path => ReferencesScript(path, identity))];
	}

	/// <summary>
	/// Collects every plain resource file in the project, straight from the editor's in-memory index.
	/// </summary>
	/// <returns>The candidate resource paths.</returns>
	/// <remarks>
	/// Anything with a more specific base type is an engine resource and cannot be a scripted one, so filtering on
	/// <c>"Resource"</c> prunes most of the project for free.
	/// </remarks>
	public static List<string> CollectResourceCandidates()
	{
		return ProjectFileIndex.CollectByType("Resource", ".tres", ".res");
	}

	/// <summary>
	/// Resolves a global class name to the script identity a resource file would reference.
	/// </summary>
	/// <param name="globalClassName">The registered global class name.</param>
	/// <param name="fallbackUid">The UID to assume when the class list has no entry yet.</param>
	/// <returns>The script's UID and path.</returns>
	public static ScriptIdentity ResolveScriptIdentity(string globalClassName, string fallbackUid)
	{
		foreach (GodotDictionary entry in ProjectSettings.GetGlobalClassList())
		{
			if (!entry.TryGetValue("class", out Variant className)
				|| className.AsString() != globalClassName
				|| !entry.TryGetValue("path", out Variant classPath))
			{
				continue;
			}

			string scriptPath = classPath.AsString();
			long id = ResourceLoader.GetResourceUid(scriptPath);

			return new ScriptIdentity(
				id == ResourceUid.InvalidId ? fallbackUid : ResourceUid.IdToText(id),
				scriptPath);
		}

		return new ScriptIdentity(fallbackUid, string.Empty);
	}

	/// <summary>
	/// Determines whether a resource file references a script, by reading only its dependency header.
	/// </summary>
	/// <param name="resourcePath">The resource file to inspect.</param>
	/// <param name="identity">The script to look for.</param>
	/// <returns><see langword="true"/> when the resource is scripted by it.</returns>
	public static bool ReferencesScript(string resourcePath, ScriptIdentity identity)
	{
		foreach (string dependency in ResourceLoader.GetDependencies(resourcePath))
		{
			// Dependencies come through as "uid::type::fallback_path", with any part possibly empty.
			string[] slices = dependency.Split("::");

			if (!string.IsNullOrEmpty(identity.Uid)
				&& string.Equals(slices[0], identity.Uid, StringComparison.Ordinal))
			{
				return true;
			}

			if (!string.IsNullOrEmpty(identity.Path)
				&& string.Equals(slices[^1], identity.Path, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}
#endif
