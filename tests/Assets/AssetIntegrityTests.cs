// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using GdUnit4;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Assets;

/// <summary>
/// Every resource and scene committed to this repository has to load. A renamed namespace or a moved script breaks
/// these silently - the file still parses, but its script reference dangles and the asset comes back null.
/// </summary>
[TestSuite]
public class AssetIntegrityTests
{
	/// <summary>
	/// Gets one case per committed resource file.
	/// </summary>
	public static IEnumerable<object[]> Resources =>
		ProjectFiles.ResourcePaths(".tres").Select(path => new object[] { path });

	/// <summary>
	/// Gets one case per committed scene file.
	/// </summary>
	public static IEnumerable<object[]> Scenes =>
		ProjectFiles.ResourcePaths(".tscn").Select(path => new object[] { path });

	[TestCase]
	[DataPoint(nameof(Resources))]
	[RequireGodotRuntime]
	public void Every_resource_loads(string resourcePath)
	{
		ResourceLoader.Load(resourcePath).Should().NotBeNull($"'{resourcePath}' has to load.");
	}

	[TestCase]
	[DataPoint(nameof(Scenes))]
	[RequireGodotRuntime]
	public void Every_scene_loads_and_instantiates(string scenePath)
	{
		PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath);

		scene.Should().NotBeNull($"'{scenePath}' has to load.");
		scene.CanInstantiate().Should().BeTrue($"'{scenePath}' has to be instantiable.");
	}
}
