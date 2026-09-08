// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using GdUnit4;

namespace Gamesmiths.Forge.Godot.Tests.Docs;

/// <summary>
/// Keeps the documentation honest about the code it describes. Docs drift silently: a renamed resolver or a moved page
/// leaves the markdown rendering perfectly while pointing at something that no longer exists.
/// </summary>
/// <remarks>
/// Each check sweeps every page inside a single test. The assertion is the same for every page, so a case per page
/// would inflate the test count without adding information - the failure message lists every offender either way.
/// Neither check needs the Godot runtime, so they run in the fast tier.
/// </remarks>
[TestSuite]
public partial class DocumentationTests
{
	// "> **Type:** `Some.Namespace.SomeResource`" - the type banner each resolver and node page opens with.
	private static readonly Regex _typeBanner = TypeBannerRegex();

	// A relative markdown link, ignoring external URLs and pure anchors.
	private static readonly Regex _relativeLink = RelativeLinkRegex();

	[TestCase]
	public void Every_documented_type_still_exists()
	{
		IEnumerable<string> missing = DocumentationPages()
			.SelectMany(page => _typeBanner.Matches(File.ReadAllText(page))
				.Select(match => (Page: page, Type: match.Groups["type"].Value)))
			.Where(entry => !PluginTypes.ExistsInPluginOrCore(entry.Type))
			.Select(entry => $"{ProjectFiles.ToResourcePath(entry.Page)} -> {entry.Type}");

		missing.Should().BeEmpty("a page documenting a type that no longer exists sends readers nowhere.");
	}

	[TestCase]
	public void Every_relative_documentation_link_resolves()
	{
		IEnumerable<string> broken = DocumentationPages()
			.SelectMany(page => _relativeLink.Matches(File.ReadAllText(page))
				.Select(match => (Page: page, Link: match.Groups["link"].Value)))
			.Where(entry => !File.Exists(Path.Combine(Path.GetDirectoryName(entry.Page)!, entry.Link)))
			.Select(entry => $"{ProjectFiles.ToResourcePath(entry.Page)} -> {entry.Link}");

		broken.Should().BeEmpty("a moved page leaves every link to it rendering fine and going nowhere.");
	}

	/// <summary>
	/// Every documentation page except the authoring templates, whose placeholders are deliberately not real types.
	/// </summary>
	private static IEnumerable<string> DocumentationPages()
	{
		return ProjectFiles.Files(".md")
			.Where(path => path.Contains(
				$"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}",
				StringComparison.Ordinal) && !path.Contains(
				$"{Path.DirectorySeparatorChar}templates{Path.DirectorySeparatorChar}",
				StringComparison.Ordinal));
	}

	[GeneratedRegex(@"^>\s*\*\*Type:\*\*\s*`(?<type>[A-Za-z0-9_.]+)`", RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex TypeBannerRegex();

	[GeneratedRegex(@"\]\((?<link>[^)#:]+\.md)(?<anchor>#[^)]*)?\)", RegexOptions.Compiled)]
	private static partial Regex RelativeLinkRegex();
}
