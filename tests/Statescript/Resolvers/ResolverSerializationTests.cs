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
/// <remarks>
/// Each property is given a non-default value before saving. Round-tripping a freshly constructed resource would prove
/// nothing: every value would already be its default, so a property that failed to serialize would come back looking
/// exactly right.
/// </remarks>
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

		foreach ((string Name, Variant.Type Type) property in ExportedProperties(original))
		{
			if (SampleValue(property.Type) is Variant sample)
			{
				original.Set(property.Name, sample);
			}
		}

		// Read back after setting rather than remembering what was written: a setter is free to clamp or normalize,
		// and the round trip only has to preserve whatever actually landed on the resource.
		//
		// Compared by value, so only the properties given a value are checked. An Object-typed export stringifies to
		// its instance id, which is different by definition after a reload, and comparing those would fail whenever a
		// resolver defaults its nested operand to a real sub-resource rather than null.
		var expected = ExportedProperties(original)
			.Where(property => SampleValue(property.Type) is not null)
			.ToDictionary(property => property.Name, property => original.Get(property.Name).ToString());

		ResourceSaver.Save(original, ScratchPath).Should().Be(Error.Ok, $"'{typeName}' has to be saveable.");

		// CacheMode.Ignore, or Godot hands back the instance just saved and the test proves nothing.
		StatescriptResolverResource reloaded = ResourceLoader.Load<StatescriptResolverResource>(
			ScratchPath,
			cacheMode: ResourceLoader.CacheMode.Ignore);

		reloaded.Should().NotBeNull($"'{typeName}' has to load back from disk.");
		reloaded.GetType().Should().Be(original.GetType(), "the reloaded resource has to keep its script.");
		reloaded.ResolverTypeId.Should().Be(original.ResolverTypeId);

		foreach (KeyValuePair<string, string> property in expected)
		{
			reloaded.Get(property.Key).ToString().Should().Be(
				property.Value,
				$"'{typeName}.{property.Key}' is exported, so its value has to survive the round trip.");
		}
	}

	/// <summary>
	/// A non-default value for a property of the given type, or <see langword="null"/> when this suite has nothing
	/// meaningful to put there.
	/// </summary>
	/// <remarks>
	/// Object-typed exports are skipped: those are nested resolvers and other resources, which cannot be synthesized
	/// without knowing what each one expects. Collections are skipped for the same reason - their element type is not
	/// visible here.
	/// </remarks>
	/// <param name="type">The Variant type of the property.</param>
	/// <returns>A representative non-default value, or <see langword="null"/> to leave the property alone.</returns>
	private static Variant? SampleValue(Variant.Type type)
	{
		return type switch
		{
			Variant.Type.Bool => true,
			Variant.Type.Int => 7,
			Variant.Type.Float => 1.5f,
			Variant.Type.String => "forge-round-trip",
			Variant.Type.StringName => new StringName("forge-round-trip"),
			Variant.Type.NodePath => new NodePath("Some/Node"),
			Variant.Type.Vector2 => new Vector2(1, 2),
			Variant.Type.Vector2I => new Vector2I(1, 2),
			Variant.Type.Vector3 => new Vector3(1, 2, 3),
			Variant.Type.Vector3I => new Vector3I(1, 2, 3),
			Variant.Type.Vector4 => new Vector4(1, 2, 3, 4),
			Variant.Type.Vector4I => new Vector4I(1, 2, 3, 4),
			Variant.Type.Color => new Color(0.25f, 0.5f, 0.75f),
			Variant.Type.Rect2 => new Rect2(1, 2, 3, 4),
			Variant.Type.Quaternion => new Quaternion(0, 0, 0, 1),
			_ => null,
		};
	}

	/// <summary>
	/// The names and Variant types of the properties Godot will actually serialize for this resource.
	/// </summary>
	/// <param name="resource">The resource to inspect.</param>
	/// <returns>The exported property names paired with their Variant types.</returns>
	private static List<(string Name, Variant.Type Type)> ExportedProperties(Resource resource)
	{
		List<(string Name, Variant.Type Type)> exported = [];

		foreach (GodotCollections.Dictionary property in resource.GetPropertyList())
		{
			var usage = (PropertyUsageFlags)(int)property["usage"];

			if (usage.HasFlag(PropertyUsageFlags.Storage) && usage.HasFlag(PropertyUsageFlags.Editor))
			{
				exported.Add((property["name"].AsString(), (Variant.Type)(int)property["type"]));
			}
		}

		return exported;
	}
}
