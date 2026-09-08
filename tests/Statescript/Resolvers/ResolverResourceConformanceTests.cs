// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using GdUnit4;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Resolvers;

/// <summary>
/// Properties that must hold for every serialized resolver resource. These are the contract the editor relies on when
/// it matches a saved resource to its editor and builds the runtime graph, and nothing else enforces them.
/// </summary>
[TestSuite]
public class ResolverResourceConformanceTests
{
	/// <summary>
	/// Gets one case per concrete resolver resource, identified by type name.
	/// </summary>
	public static IEnumerable<object[]> ResolverResources =>
		PluginTypes.ConcreteSubclassesOf(typeof(StatescriptResolverResource))
			.Select(type => new object[] { type.FullName! });

	[TestCase]
	[DataPoint(nameof(ResolverResources))]
	[RequireGodotRuntime]
	public void Every_resolver_resource_constructs_and_is_authorable(string typeName)
	{
		Type type = PluginTypes.Resolve(typeName);
		var resource = (StatescriptResolverResource)Activator.CreateInstance(type)!;

		resource.ResolverTypeId.Should().NotBeNullOrWhiteSpace(
			$"'{typeName}' has to declare a ResolverTypeId matching its editor, or it can never be re-opened.");
		type.GetCustomAttributes(typeof(GlobalClassAttribute), inherit: false).Should().NotBeEmpty(
			$"'{typeName}' needs [GlobalClass] to be creatable from the editor.");
		type.GetCustomAttributes(typeof(ToolAttribute), inherit: false).Should().NotBeEmpty(
			$"'{typeName}' needs [Tool] to run inside the editor.");
	}

	[TestCase]
	[DataPoint(nameof(ResolverResources))]
	[RequireGodotRuntime]
	public void Every_resolver_resource_rejects_an_unconfigured_build_without_dereferencing_null(string typeName)
	{
		var resource = (StatescriptResolverResource)Activator.CreateInstance(PluginTypes.Resolve(typeName))!;

		// A nested resolver is entitled to reject operands it was never given, and most do. What none of them may do
		// is dereference the missing operand: that surfaces in the editor as a bare crash with nothing to act on.
		Func<IPropertyResolver> build = () => resource.BuildResolver(new Graph());

		build.Should().NotThrow<NullReferenceException>();
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Resolver_type_ids_are_unique()
	{
		List<(string TypeName, string ResolverTypeId)> declared = [.. ResolverResources
			.Select(data => (string)data[0])
			.Select(typeName => (
				TypeName: typeName,
				((StatescriptResolverResource)Activator.CreateInstance(
					PluginTypes.Resolve(typeName))!).ResolverTypeId))];

		IEnumerable<string> duplicates = declared
			.GroupBy(entry => entry.ResolverTypeId, StringComparer.Ordinal)
			.Where(group => group.Count() > 1)
			.Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => entry.TypeName))}");

		duplicates.Should().BeEmpty(
			"a ResolverTypeId is how a saved resource finds its editor, so a collision silently loads the wrong one.");
	}
}
