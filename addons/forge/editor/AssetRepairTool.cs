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
	/// <returns>What was found, and what could not be inspected.</returns>
	internal static RepairReport Scan()
	{
		return Process(applyChanges: false);
	}

	/// <summary>
	/// Strips unregistered tags from every scene and resource in the project, and saves what it changed.
	/// </summary>
	/// <returns>What was actually repaired, and what could not be inspected.</returns>
	internal static RepairReport Apply()
	{
		return Process(applyChanges: true);
	}

	/// <summary>
	/// Every way a tag key is stored in an asset.
	/// </summary>
	/// <remarks>
	/// Not just <c>ForgeTag</c> and <c>ForgeTagContainer</c>: several types keep a tag key as a plain string, and a
	/// repair that ignored them would leave exactly the dangling references it claims to remove. Anything that gains a
	/// tag-valued property belongs in this list.
	/// </remarks>
	private static List<TagPropertyDescriptor> BuildDescriptors()
	{
		return
		[
			Describe(nameof(ForgeTagContainer), ForgeTagContainerScriptUid, "ContainerTags", isList: true),
			Describe(nameof(ForgeTag), ForgeTagScriptUid, "Tag", isList: false),
			Describe("TagResolverResource", string.Empty, "Tags", isList: true),
			Describe("SetByCallerMagnitudeResolverResource", string.Empty, "IdentifierTag", isList: false),
			Describe("AbilityCooldownResolverResource", string.Empty, "CooldownTag", isList: false),

			// ForgeCueHandler is a node type users derive from, so the script written into a scene is their own and
			// cannot be resolved in advance. The property name is distinctive enough to match on by itself.
			new TagPropertyDescriptor(default, "CueTag", IsList: false, OnNodes: true),
		];
	}

	private static TagPropertyDescriptor Describe(
		string globalClassName,
		string fallbackUid,
		string propertyName,
		bool isList)
	{
		return new TagPropertyDescriptor(
			TagsSourceScanner.ResolveScriptIdentity(globalClassName, fallbackUid),
			propertyName,
			isList,
			OnNodes: false);
	}

	private static RepairReport Process(bool applyChanges)
	{
		// Tags resolve against every configured source, so which source declares a tag is irrelevant here.
		TagsManager tagsManager = ForgeTagsRegistry.MergedTags;
		List<TagPropertyDescriptor> descriptors = BuildDescriptors();
		var findings = new List<RepairFinding>();
		var skippedAssets = new List<string>();

		List<string> assets =
		[
			.. ProjectFileIndex.CollectByType("PackedScene", ".tscn", ".scn"),
			.. ProjectFileIndex.CollectByType("Resource", ".tres", ".res"),
		];

		foreach (string assetPath in assets)
		{
			if (!IsTextAsset(assetPath))
			{
				// A binary asset is not text, so its tags cannot be read this way.
				skippedAssets.Add(assetPath);
				continue;
			}

			ProcessAsset(assetPath, tagsManager, descriptors, applyChanges, findings);
		}

		return new RepairReport(findings, skippedAssets);
	}

	private static bool IsTextAsset(string assetPath)
	{
		return assetPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
			|| assetPath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase);
	}

	private static void ProcessAsset(
		string assetPath,
		TagsManager tagsManager,
		List<TagPropertyDescriptor> descriptors,
		bool applyChanges,
		List<RepairFinding> findings)
	{
		List<string>? lines = ReadLines(assetPath);

		if (lines is null)
		{
			GD.PrintErr($"Failed to read asset: {assetPath}.");
			return;
		}

		List<AssetTagReference> references = AssetTagParser.Parse(lines, descriptors);
		var assetFindings = new List<RepairFinding>();
		bool modified = false;

		foreach (AssetTagReference reference in references)
		{
			string[] kept = [.. reference.Tags.Where(tag => IsRegistered(tagsManager, tag))];

			foreach (string tag in reference.Tags.Where(tag => !IsRegistered(tagsManager, tag)))
			{
				assetFindings.Add(new RepairFinding(assetPath, reference.Location, tag));
			}

			if (!applyChanges || kept.Length == reference.Tags.Length)
			{
				continue;
			}

			lines[reference.LineIndex] = AssetTagParser.BuildLine(reference, kept);
			modified = true;
		}

		// A failed write leaves every tag exactly where it was, so reporting them as repaired would be a lie - and the
		// caller counts what comes back as the number of references it fixed.
		if (applyChanges && modified && !WriteLines(assetPath, lines))
		{
			return;
		}

		findings.AddRange(assetFindings);

		if (applyChanges && modified)
		{
			GD.Print($"Repaired tag references in {assetPath}.");
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
