// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Tags;
using GdUnit4;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

[TestSuite]
public class ForgeNetworkOverlayTests
{
	[TestCase]
	public void The_tag_report_lists_the_tags_sent_most_sent_first()
	{
		var tagsManager = new TagsManager(["effect.fire", "effect.wet", "state.awake"]);
		var writer = new BitWriter();
		var fire = Tag.RequestTag(tagsManager, "effect.fire");
		var wet = Tag.RequestTag(tagsManager, "effect.wet");

		Tag.NetSerialize(tagsManager, fire, writer);
		Tag.NetSerialize(tagsManager, wet, writer);
		Tag.NetSerialize(tagsManager, wet, writer);

		string[] lines = ForgeNetworkOverlay.TagUsageReport(tagsManager).Split('\n');

		lines.Should().HaveCount(3, "a tag never sent is left out");
		lines[0].Should().Contain("forge/networking/commonly_replicated_tags");
		lines[1].Trim().Should().Be("2  effect.wet");
		lines[2].Trim().Should().Be("1  effect.fire");
	}
}
