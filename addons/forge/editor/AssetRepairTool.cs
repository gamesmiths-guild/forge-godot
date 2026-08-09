// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;

using GodotDictionary = Godot.Collections.Dictionary;
using GodotStringArray = Godot.Collections.Array<string>;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Finds and strips tag references that no configured tag source declares any more.
/// </summary>
/// <remarks>
/// Scenes are never loaded or instantiated - they are read, and rewritten, as text. Resources are loaded, but only the
/// few that a dependency check says could hold a tag, and only their scripted properties are read. Both restrictions
/// exist because touching an engine resource outside a running scene can be fatal: see <see cref="SceneTagParser"/>
/// and <see cref="_customClassNames"/>.
/// </remarks>
[Tool]
public partial class AssetRepairTool : EditorPlugin
{
	/// <summary>
	/// A tag reference that no longer resolves against the project's registered tags, and would therefore be stripped
	/// by a repair.
	/// </summary>
	/// <param name="AssetPath">The scene or resource holding the reference.</param>
	/// <param name="Location">Where in that asset the reference lives.</param>
	/// <param name="Tag">The unregistered tag.</param>
	public readonly record struct RepairFinding(string AssetPath, string Location, string Tag);

	/// <summary>
	/// A resource reached through a property, kept together so a finding can say where in the asset it lives.
	/// </summary>
	/// <param name="PropertyName">The property the resource was found on.</param>
	/// <param name="Resource">The resource itself.</param>
	private readonly record struct StoredResource(string PropertyName, Resource Resource);

	// Fallbacks for the moment before the global class list exists, which is only the very first build.
	private const string ForgeTagScriptUid = "uid://dpakv7agvir6y";
	private const string ForgeTagContainerScriptUid = "uid://cw525n4mjqgw0";

	/// <summary>
	/// The project's custom (scripted) global class names, used to decide which properties are safe to read.
	/// </summary>
	/// <remarks>
	/// Reading a property is not free of consequences: asking an engine resource for its value can make it resolve
	/// state it cannot resolve outside a running scene - a <c>ViewportTexture</c> being the case that crashes the
	/// editor here. Only Forge's own data can hold a tag, and Forge's data is all scripted, so restricting the walk to
	/// properties that declare a scripted class means engine resources are never touched at all.
	/// </remarks>
	private static readonly HashSet<string> _customClassNames = new(StringComparer.Ordinal);

	/// <summary>
	/// Reports every tag reference a repair would remove, without modifying anything.
	/// </summary>
	/// <returns>The tags that would be stripped.</returns>
	public static List<RepairFinding> Scan()
	{
		return Process(applyChanges: false);
	}

	/// <summary>
	/// Strips unregistered tags from every scene and resource in the project, and saves what it changed.
	/// </summary>
	/// <returns>The tags that were removed.</returns>
	public static List<RepairFinding> Apply()
	{
		return Process(applyChanges: true);
	}

	private static List<RepairFinding> Process(bool applyChanges)
	{
		// Tags resolve against every configured source, so which source declares a tag is irrelevant here.
		TagsManager tagsManager = ForgeTagsRegistry.MergedTags;
		var findings = new List<RepairFinding>();

		ScriptIdentity[] tagScripts =
		[
			TagsSourceScanner.ResolveScriptIdentity(nameof(ForgeTag), ForgeTagScriptUid),
			TagsSourceScanner.ResolveScriptIdentity(nameof(ForgeTagContainer), ForgeTagContainerScriptUid),
		];

		RefreshCustomClassNames();

		var skippedAssets = new List<string>();

		ProcessResources(tagsManager, tagScripts, applyChanges, findings);
		ProcessScenes(tagsManager, tagScripts, skippedAssets, applyChanges, findings);

		ReportSkipped(skippedAssets);

		return findings;
	}

	private static void RefreshCustomClassNames()
	{
		_customClassNames.Clear();

		foreach (GodotDictionary entry in ProjectSettings.GetGlobalClassList())
		{
			if (entry.TryGetValue("class", out Variant className))
			{
				_customClassNames.Add(className.AsString());
			}
		}
	}

	/// <summary>
	/// Determines whether a property declares a scripted class, and is therefore safe and worth reading.
	/// </summary>
	/// <param name="propertyInfo">One entry from a <c>GetPropertyList</c> call.</param>
	/// <returns><see langword="true"/> when the property may hold Forge data.</returns>
	private static bool DeclaresCustomClass(GodotDictionary propertyInfo)
	{
		if (propertyInfo.TryGetValue("class_name", out Variant className)
			&& _customClassNames.Contains(className.AsString()))
		{
			return true;
		}

		if (!propertyInfo.TryGetValue("hint_string", out Variant hint))
		{
			return false;
		}

		// A typed array declares its element type at the end of the hint, as in "24/17:ForgeModifier".
		string hintString = hint.AsString();
		int separator = hintString.LastIndexOf(':');

		return _customClassNames.Contains(separator >= 0 ? hintString[(separator + 1)..] : hintString);
	}

	private static void ReportSkipped(List<string> skippedAssets)
	{
		if (skippedAssets.Count == 0)
		{
			return;
		}

		GD.PushWarning(
			$"Skipped {skippedAssets.Count} asset(s) whose tags could not be read: "
			+ string.Join(", ", skippedAssets));
	}

	/// <summary>
	/// Determines whether an asset is worth loading, by checking its dependency header for a tag-bearing script.
	/// </summary>
	/// <param name="assetPath">The asset to test.</param>
	/// <param name="tagScripts">The scripts that can hold a tag reference.</param>
	/// <returns><see langword="true"/> when the asset could contain tags.</returns>
	/// <remarks>
	/// Loading every asset in a project to look for tags pulls in each one's whole dependency graph - meshes,
	/// textures, audio - which is both enormously slow and enough to exhaust memory on a large project. Reading the
	/// dependency header first narrows it to the handful of assets that embed a tag. Assets that merely reference an
	/// external tag resource are skipped safely, because that resource is itself scanned as its own file.
	/// </remarks>
	private static bool MayContainTags(string assetPath, ScriptIdentity[] tagScripts)
	{
		return Array.Exists(tagScripts, script => TagsSourceScanner.ReferencesScript(assetPath, script));
	}

	private static void ProcessResources(
		TagsManager tagsManager,
		ScriptIdentity[] tagScripts,
		bool applyChanges,
		List<RepairFinding> findings)
	{
		foreach (string resourcePath in ProjectFileIndex.CollectByType("Resource", ".tres", ".res"))
		{
			if (!MayContainTags(resourcePath, tagScripts))
			{
				continue;
			}

			var resource = ResourceLoader.Load<Resource>(resourcePath);

			if (resource is null)
			{
				continue;
			}

			var resourceFindings = new List<RepairFinding>();
			var visited = new HashSet<ulong>();

			CollectFindings(resource, resourcePath, string.Empty, tagsManager, resourceFindings, visited);

			findings.AddRange(resourceFindings);

			if (!applyChanges || resourceFindings.Count == 0)
			{
				continue;
			}

			RepairResource(resource, resourcePath, tagsManager);
		}
	}

	private static void RepairResource(Resource resource, string resourcePath, TagsManager tagsManager)
	{
		var visited = new HashSet<ulong>();
		var externallySaved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		StripUnregisteredTags(resource, tagsManager, visited, externallySaved);

		Error error = ResourceSaver.Save(resource, resourcePath);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to save repaired resource '{resourcePath}': {error}.");
			return;
		}

		GD.Print($"Repaired tag references in {resourcePath}.");
	}

	/// <summary>
	/// Reads and, when repairing, rewrites the tags written into each scene file.
	/// </summary>
	/// <remarks>
	/// Scenes are handled as text rather than loaded. See <see cref="SceneTagParser"/> for why that is the only safe
	/// option, and why it is also what lets entity base tags be checked at all.
	/// </remarks>
	/// <param name="tagsManager">The project's merged tag sources.</param>
	/// <param name="tagScripts">The scripts that define tags.</param>
	/// <param name="skippedAssets">Collects scenes whose tags could not be read.</param>
	/// <param name="applyChanges">Whether to rewrite the scene files.</param>
	/// <param name="findings">A list to collect any repair findings.</param>
	private static void ProcessScenes(
		TagsManager tagsManager,
		ScriptIdentity[] tagScripts,
		List<string> skippedAssets,
		bool applyChanges,
		List<RepairFinding> findings)
	{
		foreach (string scenePath in ProjectFileIndex.CollectByType("PackedScene", ".tscn", ".scn"))
		{
			if (!MayContainTags(scenePath, tagScripts))
			{
				continue;
			}

			if (!scenePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
			{
				// A binary scene is not text, so its tags cannot be read this way.
				skippedAssets.Add(scenePath);
				continue;
			}

			ProcessSceneFile(scenePath, tagsManager, tagScripts, applyChanges, findings);
		}
	}

	private static void ProcessSceneFile(
		string scenePath,
		TagsManager tagsManager,
		ScriptIdentity[] tagScripts,
		bool applyChanges,
		List<RepairFinding> findings)
	{
		List<string>? lines = ReadLines(scenePath);

		if (lines is null)
		{
			GD.PrintErr($"Failed to read scene: {scenePath}.");
			return;
		}

		List<SceneTagReference> references = SceneTagParser.Parse(lines, tagScripts[0], tagScripts[1]);
		bool modified = false;

		foreach (SceneTagReference reference in references)
		{
			string[] kept = [.. reference.Tags.Where(tag => IsRegistered(tagsManager, tag))];

			foreach (string tag in reference.Tags.Where(tag => !IsRegistered(tagsManager, tag)))
			{
				findings.Add(new RepairFinding(scenePath, reference.Location, tag));
			}

			if (!applyChanges || kept.Length == reference.Tags.Length)
			{
				continue;
			}

			lines[reference.LineIndex] = SceneTagParser.BuildLine(reference, kept);
			modified = true;
		}

		if (modified && WriteLines(scenePath, lines))
		{
			GD.Print($"Repaired tag references in {scenePath}.");
		}
	}

	private static List<string>? ReadLines(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

		return file is null ? null : [.. file.GetAsText(true).Split('\n')];
	}

	private static bool WriteLines(string path, List<string> lines)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

		if (file is null)
		{
			GD.PrintErr($"Failed to write scene: {path}.");
			return false;
		}

		file.StoreString(string.Join('\n', lines));

		return true;
	}

	private static void CollectFindingsFromValue(
		Variant value,
		string assetPath,
		string location,
		TagsManager tagsManager,
		List<RepairFinding> findings,
		HashSet<ulong> visited)
	{
		switch (value.VariantType)
		{
			case Variant.Type.Object:
				if (value.As<Resource>() is Resource resource && IsInspectableResource(resource))
				{
					CollectFindings(resource, assetPath, location, tagsManager, findings, visited);
				}

				break;

			case Variant.Type.Array:
				foreach (Variant element in value.AsGodotArray())
				{
					CollectFindingsFromValue(element, assetPath, location, tagsManager, findings, visited);
				}

				break;
		}
	}

	private static void CollectFindings(
		Resource? resource,
		string assetPath,
		string location,
		TagsManager tagsManager,
		List<RepairFinding> findings,
		HashSet<ulong> visited)
	{
		if (resource is null || !visited.Add(resource.GetInstanceId()))
		{
			return;
		}

		switch (resource)
		{
			case ForgeTagContainer container:
				foreach (string tag in container.ContainerTags ?? [])
				{
					if (!IsRegistered(tagsManager, tag))
					{
						findings.Add(new RepairFinding(assetPath, location, tag));
					}
				}

				return;

			case ForgeTag forgeTag when !IsRegistered(tagsManager, forgeTag.Tag):
				findings.Add(new RepairFinding(assetPath, location, forgeTag.Tag));
				return;

			case ForgeTag:
			case ForgeTagsSource:
				return;
		}

		foreach (StoredResource nested in EnumerateStoredResources(resource))
		{
			CollectFindings(
				nested.Resource,
				assetPath,
				CombineLocation(location, nested.PropertyName),
				tagsManager,
				findings,
				visited);
		}
	}

	private static void StripUnregisteredTags(
		Resource? resource,
		TagsManager tagsManager,
		HashSet<ulong> visited,
		HashSet<string> externallySaved)
	{
		if (resource is null || !visited.Add(resource.GetInstanceId()))
		{
			return;
		}

		switch (resource)
		{
			case ForgeTagContainer container:
				StripContainer(container, tagsManager, externallySaved);
				return;

			case ForgeTag forgeTag:
				StripTag(forgeTag, tagsManager, externallySaved);
				return;

			case ForgeTagsSource:
				return;
		}

		foreach (StoredResource nested in EnumerateStoredResources(resource))
		{
			StripUnregisteredTags(nested.Resource, tagsManager, visited, externallySaved);
		}
	}

	private static void StripContainer(
		ForgeTagContainer container,
		TagsManager tagsManager,
		HashSet<string> externallySaved)
	{
		if (container.ContainerTags is null)
		{
			return;
		}

		var keptTags = new GodotStringArray();
		bool modified = false;

		foreach (string tag in container.ContainerTags)
		{
			if (IsRegistered(tagsManager, tag))
			{
				keptTags.Add(tag);
			}
			else
			{
				modified = true;
			}
		}

		if (!modified)
		{
			return;
		}

		container.ContainerTags = keptTags;
		SaveIfStandalone(container, externallySaved);
	}

	private static void StripTag(ForgeTag forgeTag, TagsManager tagsManager, HashSet<string> externallySaved)
	{
		if (IsRegistered(tagsManager, forgeTag.Tag))
		{
			return;
		}

		forgeTag.Tag = string.Empty;
		SaveIfStandalone(forgeTag, externallySaved);
	}

	/// <summary>
	/// Writes a repaired resource out when it lives in its own file.
	/// </summary>
	/// <param name="resource">The repaired resource.</param>
	/// <param name="externallySaved">Paths already written during this run, so a shared file is saved once.</param>
	/// <remarks>
	/// A resource with its own path can be shared by any number of scenes, and saving the scene that happened to
	/// reference it would not write the shared file at all - the fix would live only in memory until something else
	/// reloaded it away. Built-in sub-resources need no special handling: they are serialized with their owner.
	/// </remarks>
	private static void SaveIfStandalone(Resource resource, HashSet<string> externallySaved)
	{
		string path = resource.ResourcePath;

		if (string.IsNullOrEmpty(path) || path.Contains("::", StringComparison.Ordinal))
		{
			return;
		}

		if (!externallySaved.Add(path))
		{
			return;
		}

		Error error = ResourceSaver.Save(resource, path);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to save repaired resource '{path}': {error}.");
			return;
		}

		GD.Print($"Repaired tag references in {path}.");
	}

	private static IEnumerable<StoredResource> EnumerateStoredResources(GodotObject owner)
	{
		foreach (GodotDictionary propertyInfo in owner.GetPropertyList())
		{
			if (!propertyInfo.TryGetValue("usage", out Variant usage)
				|| ((PropertyUsageFlags)usage.AsInt64() & PropertyUsageFlags.Storage) == 0
				|| !propertyInfo.TryGetValue("name", out Variant name))
			{
				continue;
			}

			string propertyName = name.AsString();

			// A script is itself a Resource, so following it would walk out of the asset and into the engine's own
			// object graph - which is unbounded, and has no tags in it.
			if (propertyName == "script" || !DeclaresCustomClass(propertyInfo))
			{
				continue;
			}

			foreach (Resource resource in EnumerateResourcesInValue(owner.Get(propertyName)))
			{
				yield return new StoredResource(propertyName, resource);
			}
		}
	}

	private static string CombineLocation(string location, string propertyName)
	{
		return string.IsNullOrEmpty(location) ? propertyName : $"{location}/{propertyName}";
	}

	/// <summary>
	/// Determines whether a resource is one this tool may inspect.
	/// </summary>
	/// <param name="resource">The resource reached through a property.</param>
	/// <returns><see langword="true"/> when it is a custom resource worth walking into.</returns>
	/// <remarks>
	/// Only scripted resources can carry Forge tags, so engine resources are skipped - and skipping them is not just
	/// an optimization. Reading the properties of some built-in types outside a live scene tree is actively unsafe:
	/// a ViewportTexture, for instance, resolves its viewport path on access, which logs "Path to node is invalid"
	/// the first time and takes the editor down when the same resource is walked again.
	/// </remarks>
	private static bool IsInspectableResource(Resource resource)
	{
		return resource is not Script && resource.GetScript().VariantType != Variant.Type.Nil;
	}

	private static IEnumerable<Resource> EnumerateResourcesInValue(Variant value)
	{
		switch (value.VariantType)
		{
			case Variant.Type.Object:
				if (value.As<Resource>() is Resource resource && IsInspectableResource(resource))
				{
					yield return resource;
				}

				break;

			case Variant.Type.Array:
				foreach (Variant element in value.AsGodotArray())
				{
					foreach (Resource nested in EnumerateResourcesInValue(element))
					{
						yield return nested;
					}
				}

				break;
		}
	}

	private static bool IsRegistered(TagsManager tagsManager, string? tagKey)
	{
		// An unset tag is not a dangling reference, just an empty slot; removing it would change nothing.
		if (string.IsNullOrWhiteSpace(tagKey))
		{
			return true;
		}

		try
		{
			Tag.RequestTag(tagsManager, tagKey);
			return true;
		}
		catch (TagNotRegisteredException)
		{
			return false;
		}
	}
}
#endif
