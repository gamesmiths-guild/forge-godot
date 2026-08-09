// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Finds and strips tag references that no configured tag source declares any more.
/// </summary>
/// <remarks>
/// Assets are read, and rewritten, as text - never loaded and never instantiated. See <see cref="AssetTagParser"/>
/// for why that is the only safe option in Godot 4.7.
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

	// Fallbacks for the moment before the global class list exists, which is only the very first build.
	private const string ForgeTagScriptUid = "uid://dpakv7agvir6y";
	private const string ForgeTagContainerScriptUid = "uid://cw525n4mjqgw0";

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
		var skippedAssets = new List<string>();

		ScriptIdentity tagScript = TagsSourceScanner.ResolveScriptIdentity(nameof(ForgeTag), ForgeTagScriptUid);
		ScriptIdentity containerScript =
			TagsSourceScanner.ResolveScriptIdentity(nameof(ForgeTagContainer), ForgeTagContainerScriptUid);

		List<string> assets =
		[
			.. ProjectFileIndex.CollectByType("PackedScene", ".tscn", ".scn"),
			.. ProjectFileIndex.CollectByType("Resource", ".tres", ".res"),
		];

		foreach (string assetPath in assets)
		{
			if (!MayContainTags(assetPath, tagScript, containerScript))
			{
				continue;
			}

			if (!IsTextAsset(assetPath))
			{
				// A binary asset is not text, so its tags cannot be read this way.
				skippedAssets.Add(assetPath);
				continue;
			}

			ProcessAsset(assetPath, tagsManager, tagScript, containerScript, applyChanges, findings);
		}

		ReportSkipped(skippedAssets);

		return findings;
	}

	private static bool IsTextAsset(string assetPath)
	{
		return assetPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
			|| assetPath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Determines whether an asset is worth reading, by checking its dependency header for a tag-bearing script.
	/// </summary>
	/// <param name="assetPath">The asset to test.</param>
	/// <param name="tagScript">The <c>ForgeTag</c> script identity.</param>
	/// <param name="containerScript">The <c>ForgeTagContainer</c> script identity.</param>
	/// <returns><see langword="true"/> when the asset could contain tags.</returns>
	/// <remarks>
	/// The dependency header is a bounded read that stops before the body, so this narrows a whole project down to the
	/// handful of files that actually embed a tag. An asset that merely references an external tag resource is passed
	/// over safely, because that resource is itself examined as its own file.
	/// </remarks>
	private static bool MayContainTags(string assetPath, ScriptIdentity tagScript, ScriptIdentity containerScript)
	{
		return TagsSourceScanner.ReferencesScript(assetPath, tagScript)
			|| TagsSourceScanner.ReferencesScript(assetPath, containerScript);
	}

	private static void ProcessAsset(
		string assetPath,
		TagsManager tagsManager,
		ScriptIdentity tagScript,
		ScriptIdentity containerScript,
		bool applyChanges,
		List<RepairFinding> findings)
	{
		List<string>? lines = ReadLines(assetPath);

		if (lines is null)
		{
			GD.PrintErr($"Failed to read asset: {assetPath}.");
			return;
		}

		List<AssetTagReference> references = AssetTagParser.Parse(lines, tagScript, containerScript);
		bool modified = false;

		foreach (AssetTagReference reference in references)
		{
			string[] kept = [.. reference.Tags.Where(tag => IsRegistered(tagsManager, tag))];

			foreach (string tag in reference.Tags.Where(tag => !IsRegistered(tagsManager, tag)))
			{
				findings.Add(new RepairFinding(assetPath, reference.Location, tag));
			}

			if (!applyChanges || kept.Length == reference.Tags.Length)
			{
				continue;
			}

			lines[reference.LineIndex] = AssetTagParser.BuildLine(reference, kept);
			modified = true;
		}

		if (modified && WriteLines(assetPath, lines))
		{
			GD.Print($"Repaired tag references in {assetPath}.");
		}
	}

	private static void ReportSkipped(List<string> skippedAssets)
	{
		if (skippedAssets.Count == 0)
		{
			return;
		}

		GD.PushWarning(
			$"Skipped {skippedAssets.Count} binary asset(s) whose tags could not be read: "
			+ string.Join(", ", skippedAssets));
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
			GD.PrintErr($"Failed to write asset: {path}.");
			return false;
		}

		file.StoreString(string.Join('\n', lines));

		return true;
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
