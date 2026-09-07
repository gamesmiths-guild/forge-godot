// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using GdUnit4;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Resolvers;

/// <summary>
/// The editor side of the resolver contract. A saved resource finds its editor by matching
/// <c>ResolverTypeId</c>, so a resource without a matching editor cannot be authored, and an editor that throws on
/// construction takes out every dropdown that offers it.
/// </summary>
[TestSuite]
public class ResolverEditorConformanceTests
{
	// Resolvers whose node editor renders their slot itself instead of offering them as a resolver row, so they
	// deliberately have no NodeEditorProperty:
	//
	//   EventPayloadOutput   EventListenerNodeEditor constructs and edits the resource directly.
	//                        StatescriptResolverRegistry documents the same exception.
	private static readonly string[] _authoredByTheirNodeEditor = ["EventPayloadOutput"];

	/// <summary>
	/// Gets one case per concrete resolver editor, identified by type name.
	/// </summary>
	public static IEnumerable<object[]> ResolverEditors =>
		PluginTypes.ConcreteSubclassesOf(typeof(NodeEditorProperty))
			.Select(type => new object[] { type.FullName! });

	[TestCase]
	[DataPoint(nameof(ResolverEditors))]
	[RequireGodotRuntime]
	public void Every_resolver_editor_constructs_and_declares_a_resolver_type_id(string typeName)
	{
		NodeEditorProperty editor = Instantiate(typeName);

		try
		{
			editor.ResolverTypeId.Should().NotBeNullOrWhiteSpace(
				$"'{typeName}' has to declare a ResolverTypeId matching its resource.");
			editor.DisplayName.Should().NotBeNullOrWhiteSpace(
				$"'{typeName}' is offered in a dropdown, so it needs a display name.");
		}
		finally
		{
			editor.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Every_resolver_resource_has_an_editor_with_a_matching_type_id()
	{
		HashSet<string> editorIds = [.. ResolverEditors
			.Select(data => (string)data[0])
			.Select(typeName =>
			{
				NodeEditorProperty editor = Instantiate(typeName);

				try
				{
					return editor.ResolverTypeId;
				}
				finally
				{
					editor.Free();
				}
			})];

		IEnumerable<string> unauthorable = PluginTypes
			.ConcreteSubclassesOf(typeof(StatescriptResolverResource))
			.Select(type => (Type: type, Id: ResolverTypeIdOf(type)))
			.Where(entry => entry.Id.Length > 0
				&& !editorIds.Contains(entry.Id)
				&& !_authoredByTheirNodeEditor.Contains(entry.Id, StringComparer.Ordinal))
			.Select(entry => $"{entry.Type.Name} (ResolverTypeId '{entry.Id}')");

		unauthorable.Should().BeEmpty(
			"a resolver resource with no editor declaring the same ResolverTypeId cannot be authored or re-opened.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Resolver_editor_type_ids_are_unique()
	{
		IEnumerable<string> duplicates = ResolverEditors
			.Select(data => (string)data[0])
			.Select(typeName =>
			{
				NodeEditorProperty editor = Instantiate(typeName);

				try
				{
					return (TypeName: typeName, Id: editor.ResolverTypeId);
				}
				finally
				{
					editor.Free();
				}
			})
			.GroupBy(entry => entry.Id, StringComparer.Ordinal)
			.Where(group => group.Count() > 1)
			.Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => entry.TypeName))}");

		duplicates.Should().BeEmpty("a duplicated ResolverTypeId makes resource-to-editor matching ambiguous.");
	}

	private static string ResolverTypeIdOf(Type resourceType)
	{
		var resource = (StatescriptResolverResource)Activator.CreateInstance(resourceType)!;
		return resource.ResolverTypeId;
	}

	private static NodeEditorProperty Instantiate(string typeName)
	{
		return (NodeEditorProperty)Activator.CreateInstance(PluginTypes.Resolve(typeName))!;
	}
}
#endif
