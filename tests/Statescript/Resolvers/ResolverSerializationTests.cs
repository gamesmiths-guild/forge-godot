// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using GdUnit4;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Resolvers;

/// <summary>
/// Resolver resources are saved inside graph <c>.tres</c> files, so every exported property has to survive a save and
/// a reload. An export whose type Godot cannot serialize is silently written as a default and the authored value is
/// gone the next time the graph is opened.
/// </summary>
[TestSuite]
public class ResolverSerializationTests
{
	private const string ScratchPath = "user://resolver-round-trip.tres";

	/// <summary>
	/// Gets one case per concrete resolver resource, identified by type name.
	/// </summary>
	public static IEnumerable<object[]> ResolverResources =>
		PluginTypes.ConcreteSubclassesOf(typeof(StatescriptResolverResource))
			.Select(type => new object[] { type.FullName! });

	[TestCase]
	[DataPoint(nameof(ResolverResources))]
	[RequireGodotRuntime]
	public void Every_resolver_resource_survives_a_save_and_reload(string typeName)
	{
		var original = (StatescriptResolverResource)Activator.CreateInstance(PluginTypes.Resolve(typeName))!;

		ResourceSaver.Save(original, ScratchPath).Should().Be(Error.Ok, $"'{typeName}' has to be saveable.");

		// CacheMode.Ignore, or Godot hands back the instance just saved and the test proves nothing.
		StatescriptResolverResource reloaded = ResourceLoader.Load<StatescriptResolverResource>(
			ScratchPath,
			cacheMode: ResourceLoader.CacheMode.Ignore);

		reloaded.Should().NotBeNull($"'{typeName}' has to load back from disk.");
		reloaded!.GetType().Should().Be(original.GetType(), "the reloaded resource has to keep its script.");
		reloaded.ResolverTypeId.Should().Be(original.ResolverTypeId);

		foreach (string property in ExportedProperties(original))
		{
			reloaded.Get(property).ToString().Should().Be(
				original.Get(property).ToString(),
				$"'{typeName}.{property}' is exported, so it has to survive the round trip.");
		}
	}

	/// <summary>
	/// The names of the properties Godot will actually serialize for this resource.
	/// </summary>
	/// <param name="resource">The resource to inspect.</param>
	private static IEnumerable<string> ExportedProperties(Resource resource)
	{
		foreach (GodotCollections.Dictionary property in resource.GetPropertyList())
		{
			var usage = (PropertyUsageFlags)(int)property["usage"];

			if (usage.HasFlag(PropertyUsageFlags.Storage) && usage.HasFlag(PropertyUsageFlags.Editor))
			{
				yield return property["name"].AsString();
			}
		}
	}
}
