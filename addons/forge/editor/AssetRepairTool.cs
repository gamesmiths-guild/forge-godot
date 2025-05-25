// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor;

[Tool]
public partial class AssetRepairTool : EditorPlugin
{
	public static void RepairAllAssetsTags()
	{
		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");
		var tagsManager = new TagsManager([.. pluginData.RegisteredTags]);

		List<string> scenes = GetScenePaths("res://");
		GD.Print($"Found {scenes.Count} scene(s) to process.");

		var openedScenes = EditorInterface.Singleton.GetOpenScenes();

		foreach (var originalScenePath in scenes)
		{
			// For some weird reason scenes from the GetScenePath are comming with 3 slahes instead of just two.
			var scenePath = originalScenePath.Replace("res:///", "res://");

			GD.Print($"Processing scene: {scenePath}.");
			PackedScene? packedScene = ResourceLoader.Load<PackedScene>(scenePath);

			if (packedScene is null)
			{
				GD.PrintErr($"Failed to load scene: {scenePath}.");
				continue;
			}

			Node sceneInstance = packedScene.Instantiate();
			var modified = ProcessNode(sceneInstance, tagsManager);

			if (!modified)
			{
				GD.Print($"No changes needed for {scenePath}.");
				continue;
			}

			// 'sceneInstance' is the modified scene instance in memory, need to save to disk and reload if needed.
			var newScene = new PackedScene();
			Error error = newScene.Pack(sceneInstance);
			if (error != Error.Ok)
			{
				GD.PrintErr($"Failed to pack scene: {error}.");
				continue;
			}

			error = ResourceSaver.Save(newScene, scenePath);
			if (error != Error.Ok)
			{
				GD.PrintErr($"Failed to save scene: {error}.");
				continue;
			}

			if (openedScenes.Contains(scenePath))
			{
				GD.Print($"Scene was opened, reloading background scene: {scenePath}.");
				EditorInterface.Singleton.ReloadSceneFromPath(scenePath);
			}
		}
	}

	/// <summary>
	/// Recursively get scene files from a folder.
	/// </summary>
	/// <param name="basePath">Current path iteration.</param>
	/// <returns>List of scenes found.</returns>
	private static List<string> GetScenePaths(string basePath)
	{
		var scenePaths = new List<string>();
		var dir = DirAccess.Open(basePath);

		if (dir is null)
		{
			GD.PrintErr($"Failed to open directory: {basePath}");
			return scenePaths;
		}

		// Start listing directory entries; skip navigational and hidden files.
		dir.ListDirBegin();
		while (true)
		{
			var fileName = dir.GetNext();
			if (string.IsNullOrEmpty(fileName))
			{
				break;
			}

			var filePath = $"{basePath}/{fileName}";
			if (dir.CurrentIsDir())
			{
				// Recursively scan subdirectories.
				scenePaths.AddRange(GetScenePaths(filePath));
			}
			else if (fileName.EndsWith(".tscn", StringComparison.InvariantCultureIgnoreCase)
				|| fileName.EndsWith(".scn", StringComparison.InvariantCultureIgnoreCase))
			{
				scenePaths.Add(filePath);
			}
		}

		dir.ListDirEnd();
		return scenePaths;
	}

	/// <summary>
	/// Recursively process nodes; returns true if any ForgeEntity was modified.
	/// </summary>
	/// <param name="node">Current node iteration.</param>
	/// <param name="tagsManager">The tags manager used to validate tags.</param>
	/// <returns><see langword="true"/> if any ForgeEntity was modified.</returns>
	private static bool ProcessNode(Node node, TagsManager tagsManager)
	{
		var modified = ValidateNode(node, tagsManager);

		foreach (Node child in node.GetChildren())
		{
			modified |= ProcessNode(child, tagsManager);
		}

		return modified;
	}

	private static bool ValidateNode(Node node, TagsManager tagsManager)
	{
		var modified = false;
		foreach (Dictionary propertyInfo in node.GetPropertyList())
		{
			if (!propertyInfo.TryGetValue("class_name", out Variant className))
			{
				continue;
			}

			if (className.AsString() != "TagContainer")
			{
				continue;
			}

			if (!propertyInfo.TryGetValue("name", out Variant nameObj))
			{
				continue;
			}

			var propertyName = nameObj.AsString();
			Variant value = node.Get(propertyName);

			if (value.VariantType != Variant.Type.Object)
			{
				continue;
			}

			if (value.As<Resource>() is ForgeTagContainer tagContainer)
			{
				modified |= ValidateTagContainerProperty(tagContainer, node.Name, tagsManager);
			}
		}

		return modified;
	}

	private static bool ValidateTagContainerProperty(ForgeTagContainer container, string nodeName, TagsManager tagsManager)
	{
		if (container.ContainerTags is null)
		{
			return false;
		}

		Array<string> originalTags = container.ContainerTags;
		var newTags = new Array<string>();
		var modified = false;

		foreach (var tag in originalTags)
		{
			try
			{
				Tag.RequestTag(tagsManager, tag);
				newTags.Add(tag);
			}
			catch (TagNotRegisteredException)
			{
				GD.PrintRich(
					$"[color=LIGHT_STEEL_BLUE][RepairTool] Removing invalid tag [{tag}] from node {nodeName}.");
				modified = true;
			}
		}

		if (modified)
		{
			container.ContainerTags = newTags;
		}

		return modified;
	}
}
#endif
