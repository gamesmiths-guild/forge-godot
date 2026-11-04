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
/// resource the editor's filesystem index knows, sorted, so every machine that builds the project writes the same
/// manifest and the session's registry hashes the same.
/// </summary>
internal static class NetworkManifestGenerator
{
	/// <summary>
	/// Regenerates the manifest at <see cref="ForgeSettings.GetNetworkManifestPath"/>, keeping whatever scenes the
	/// existing one lists.
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

		// Saved only when something changed: saving changes the filesystem, which regenerates the manifest again.
		if (ResourceLoader.Exists(path)
			&& manifest.Effects.SequenceEqual(effects)
			&& manifest.Abilities.SequenceEqual(abilities)
			&& manifest.Graphs.SequenceEqual(graphs))
		{
			manifest.TakeOverPath(path);
			return manifest;
		}

		manifest.Effects = [.. effects];
		manifest.Abilities = [.. abilities];
		manifest.Graphs = [.. graphs];

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
