// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Godot.Resources;
using GdUnit4;

namespace Gamesmiths.Forge.Godot.Tests.Tags;

/// <summary>
/// Tag sources are the authored half of the tag system: what the Tags dock edits and what gets merged into the runtime
/// tags manager. Tags match case-insensitively at runtime, and the hierarchy is implicit in the dotted key, so both
/// have to hold in the editing operations too.
/// </summary>
[TestSuite]
public class ForgeTagsSourceTests
{
	[TestCase]
	[RequireGodotRuntime]
	public void Declaring_a_tag_is_case_insensitive()
	{
		ForgeTagsSource source = SourceWith("combat.melee");

		source.DeclaresTag("Combat.Melee").Should().BeTrue("tags match case-insensitively at runtime.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_source_declares_the_ancestors_of_the_tags_it_holds()
	{
		ForgeTagsSource source = SourceWith("combat.melee.heavy");

		source.DeclaresTagOrDescendant("combat").Should().BeTrue("the parent exists implicitly through its child.");
		source.DeclaresTag("combat").Should().BeFalse("only the full key is actually declared.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_tag_sharing_a_prefix_is_not_a_descendant()
	{
		ForgeTagsSource source = SourceWith("combat.meleeattack");

		// "combat.melee" and "combat.meleeattack" are siblings; only a '.' makes a descendant.
		source.DeclaresTagOrDescendant("combat.melee").Should().BeFalse();
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Adding_a_tag_that_is_already_declared_is_a_no_op()
	{
		ForgeTagsSource source = SourceWith("combat.melee");

		source.WithTagAdded("COMBAT.MELEE").Should().BeNull(
			"a differently-cased duplicate would collide at runtime, so it is refused rather than added.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Adding_a_new_tag_appends_it()
	{
		ForgeTagsSource source = SourceWith("combat.melee");

		source.WithTagAdded("combat.ranged").Should().BeEquivalentTo("combat.melee", "combat.ranged");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Removing_a_tag_removes_its_descendants_but_not_its_siblings()
	{
		ForgeTagsSource source = SourceWith(
			"combat.melee",
			"combat.melee.heavy",
			"combat.meleeattack",
			"combat.ranged");

		source.WithTagRemoved("combat.melee").Should().BeEquivalentTo(
			["combat.meleeattack", "combat.ranged"],
			"only the tag and its dotted descendants go; a prefix sibling stays.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Applying_tags_replaces_the_whole_list()
	{
		ForgeTagsSource source = SourceWith("combat.melee", "combat.ranged");

		source.ApplyRegisteredTags(["magic.fire"]);

		source.RegisteredTags.Should().BeEquivalentTo("magic.fire");
	}

	private static ForgeTagsSource SourceWith(params string[] tags)
	{
		var source = new ForgeTagsSource();
		source.RegisteredTags.AddRange(tags);
		return source;
	}
}
