// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Networking;

/// <summary>
/// Writes the project's <see cref="ForgeNetworkManifest"/>: the UIDs of every effect, ability and Statescript graph
/// resource the editor's filesystem index knows, and of every scene with a networked <c>ForgeEntity</c> of its own or
/// from the scene it inherits, sorted, so every machine that builds the project writes the same manifest and the
/// session's registry hashes the same.
/// </summary>
internal static class NetworkManifestGenerator
{
	/// <summary>
	/// Regenerates the manifest at <see cref="ForgeSettings.GetNetworkManifestPath"/>.
	/// </summary>
	/// <returns>The manifest written, or <see langword="null"/> when it couldn't be saved.</returns>
	public static ForgeNetworkManifest? Regenerate()
	{
		string path = ForgeSettings.GetNetworkManifestPath();
		ForgeNetworkManifest manifest = ResourceLoader.Exists(path)
			? ResourceLoader.Load<ForgeNetworkManifest>(path, cacheMode: ResourceLoader.CacheMode.Ignore)
			: new ForgeNetworkManifest();

		List<string> effects = Uids(ProjectFileIndex.CollectResourcesByScriptClass(nameof(ForgeEffectData)));
		List<string> abilities = Uids(ProjectFileIndex.CollectResourcesByScriptClass("ForgeAbilityData"));
		List<string> graphs = Uids(ProjectFileIndex.CollectResourcesByScriptClass("StatescriptGraph"));
		List<string> scenes = Uids([.. ProjectFileIndex.CollectByType("PackedScene", ".tscn").Where(IsNetworkedScene)]);

		// Saved only when something changed: saving changes the filesystem, which regenerates the manifest again.
		if (ResourceLoader.Exists(path)
			&& manifest.Effects.SequenceEqual(effects)
			&& manifest.Abilities.SequenceEqual(abilities)
			&& manifest.Graphs.SequenceEqual(graphs)
			&& manifest.Scenes.SequenceEqual(scenes))
		{
			manifest.TakeOverPath(path);
			return manifest;
		}

		manifest.Effects = [.. effects];
		manifest.Abilities = [.. abilities];
		manifest.Graphs = [.. graphs];
		manifest.Scenes = [.. scenes];

		Error error = ResourceSaver.Save(manifest, path);

		if (error != Error.Ok)
		{
			GD.PushError($"Forge: the network manifest couldn't be saved to {path}: {error}.");
			return null;
		}

		manifest.TakeOverPath(path);
		return manifest;
	}

	/// <summary>
	/// Whether saving a resource changes what the manifest lists.
	/// </summary>
	/// <param name="resource">The resource saved.</param>
	/// <returns><see langword="true"/> for an effect, an ability or a graph.</returns>
	public static bool Lists(Resource resource)
	{
		return resource is ForgeEffectData
			|| resource.GetType().Name is "ForgeAbilityData" or "StatescriptGraph";
	}

	/// <summary>
	/// Whether a scene's text gives one of its own nodes a script with <c>Networked</c> on, <c>ForgeEntity</c> or a
	/// class deriving from it: a scene the server spawns networked entities from. A level that only turns it on for an
	/// instance it holds is placed, not spawned, and isn't listed.
	/// </summary>
	/// <param name="text">The text of a <c>.tscn</c> file.</param>
	/// <returns><see langword="true"/> for a networked scene.</returns>
	internal static bool IsNetworkedSceneText(string text)
	{
		bool hasScript = false;

		foreach (string raw in text.Split('\n'))
		{
			string line = raw.Trim();

			if (line.StartsWith('['))
			{
				hasScript = false;
			}
			else if (line.StartsWith("script = ", StringComparison.Ordinal))
			{
				hasScript = true;
			}
			else if (hasScript && line == "Networked = true")
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Finds the scene a scene inherits: the one its root node is an instance of.
	/// </summary>
	/// <param name="text">The text of a <c>.tscn</c> file.</param>
	/// <returns>The inherited scene's path, or <see langword="null"/> for a scene that inherits none.</returns>
	internal static string? InheritedScenePath(string text)
	{
		var resources = new Dictionary<string, string>();

		foreach (string raw in text.Split('\n'))
		{
			string line = raw.Trim();

			if (line.StartsWith("[ext_resource", StringComparison.Ordinal))
			{
				if (Attribute(line, " id=\"") is string id && Attribute(line, " path=\"") is string path)
				{
					resources[id] = path;
				}
			}
			else if (line.StartsWith("[node", StringComparison.Ordinal))
			{
				return Attribute(line, " instance=ExtResource(\"") is string id
					? resources.GetValueOrDefault(id)
					: null;
			}
		}

		return null;
	}

	// A scene that inherits another is networked when it says so itself, by its own override too, or else when the one
	// it inherits is.
	internal static bool IsNetworkedScene(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

		if (file is null)
		{
			return false;
		}

		string text = file.GetAsText();

		if (IsNetworkedSceneText(text))
		{
			return true;
		}

		if (InheritedScenePath(text) is not string inherited
			|| text.Contains("Networked = false", StringComparison.Ordinal))
		{
			return false;
		}

		return text.Contains("Networked = true", StringComparison.Ordinal) || IsNetworkedScene(inherited);
	}

	private static string? Attribute(string line, string prefix)
	{
		int start = line.IndexOf(prefix, StringComparison.Ordinal);

		if (start < 0)
		{
			return null;
		}

		start += prefix.Length;
		return line[start..line.IndexOf('"', start)];
	}

	private static List<string> Uids(List<string> paths)
	{
		var uids = new List<string>(paths.Count);

		foreach (string path in paths)
		{
			long id = ResourceLoader.GetResourceUid(path);

			if (id == ResourceUid.InvalidId)
			{
				GD.PushWarning($"Forge: {path} has no UID yet and was left out of the network manifest. Save it once.");
				continue;
			}

			uids.Add(ResourceUid.IdToText(id));
		}

		uids.Sort(StringComparer.Ordinal);
		return uids;
	}
}
#endif
