// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using GdUnit4;

namespace Gamesmiths.Forge.Godot.Tests.Assets;

/// <summary>
/// Godot identifies every script by the UID in its sibling <c>.cs.uid</c> file. A script without one gets a fresh UID
/// on the next import, and anything referencing it by UID breaks; two scripts sharing one is worse, because the
/// reference silently resolves to the wrong script.
/// </summary>
/// <remarks>
/// Both checks sweep the whole project inside a single test rather than fanning out one case per file. The assertion
/// is identical for every file, so a case per file would report a number rather than more information - the failure
/// message names every offender either way.
/// </remarks>
[TestSuite]
public class UidIntegrityTests
{
	[TestCase]
	public void Every_script_has_a_uid_file()
	{
		IEnumerable<string> missing = ProjectFiles.Files(".cs")
			.Where(path => !File.Exists($"{path}.uid"))
			.Select(ProjectFiles.ToResourcePath);

		missing.Should().BeEmpty("a script with no .cs.uid is re-issued a new UID on the next import.");
	}

	[TestCase]
	public void Uids_are_unique()
	{
		IEnumerable<string> duplicates = ProjectFiles.Files(".uid")
			.Select(path => (Path: path, Uid: File.ReadAllText(path).Trim()))
			.Where(entry => entry.Uid.Length > 0)
			.GroupBy(entry => entry.Uid, StringComparer.Ordinal)
			.Where(group => group.Count() > 1)
			.Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => Path.GetFileName(entry.Path)))}");

		duplicates.Should().BeEmpty("two files sharing a UID makes every reference to it resolve to one of them.");
	}
}
