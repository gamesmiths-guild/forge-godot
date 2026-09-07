// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using GdUnit4;
using Gamesmiths.Forge.Godot.Tests.Helpers;

namespace Gamesmiths.Forge.Godot.Tests.Docs;

/// <summary>
/// Keeps the documentation honest about the code it describes. Docs drift silently: a renamed resolver or a moved page
/// leaves the markdown rendering perfectly while pointing at something that no longer exists.
/// </summary>
/// <remarks>
/// Neither of these needs the Godot runtime, so they run in the fast tier.
/// </remarks>
[TestSuite]
public class DocumentationTests
{
	// "> **Type:** `Some.Namespace.SomeResource`" - the type banner each resolver and node page opens with.
	private static readonly Regex _typeBanner = new(
		@"^>\s*\*\*Type:\*\*\s*`(?<type>[A-Za-z0-9_.]+)`",
		RegexOptions.Multiline | RegexOptions.Compiled);

	// A relative markdown link, ignoring external URLs and pure anchors.
	private static readonly Regex _relativeLink = new(
		@"\]\((?<link>[^)#:]+\.md)(?<anchor>#[^)]*)?\)",
		RegexOptions.Compiled);

	/// <summary>
	/// Gets one case per documented type, as (page, type name).
	/// </summary>
	public static IEnumerable<object[]> DocumentedTypes =>
		DocumentationPages()
			.SelectMany(page => _typeBanner.Matches(File.ReadAllText(page))
				.Select(match => new object[]
				{
					ProjectFiles.ToResourcePath(page),
					match.Groups["type"].Value,
				}));

	/// <summary>
	/// Gets one case per relative markdown link, as (page, link target).
	/// </summary>
	public static IEnumerable<object[]> DocumentationLinks =>
		DocumentationPages()
			.SelectMany(page => _relativeLink.Matches(File.ReadAllText(page))
				.Select(match => new object[]
				{
					ProjectFiles.ToResourcePath(page),
					match.Groups["link"].Value,
				}));

	[TestCase]
	[DataPoint(nameof(DocumentedTypes))]
	public void Every_documented_type_still_exists(string page, string typeName)
	{
		PluginTypes.All.Select(type => type.FullName).Should().Contain(
			typeName,
			$"'{page}' documents '{typeName}', which no longer exists in the plugin.");
	}

	[TestCase]
	[DataPoint(nameof(DocumentationLinks))]
	public void Every_relative_documentation_link_resolves(string page, string link)
	{
		var pageDirectory = Path.GetDirectoryName(Path.Combine(ProjectFiles.ProjectRoot, page.Replace("res://", string.Empty)))!;

		File.Exists(Path.Combine(pageDirectory, link)).Should().BeTrue(
			$"'{page}' links to '{link}', which does not exist.");
	}

	/// <summary>
	/// Every documentation page except the authoring templates, whose placeholders are deliberately not real types.
	/// </summary>
	private static IEnumerable<string> DocumentationPages()
	{
		return ProjectFiles.Files(".md")
			.Where(path => path.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			.Where(path => !path.Contains($"{Path.DirectorySeparatorChar}templates{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
	}
}
