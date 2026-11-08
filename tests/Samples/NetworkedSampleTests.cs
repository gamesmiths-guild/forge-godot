// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Godot.Nodes;
using GdUnit4;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Samples;

/// <summary>
/// Sample 05's level as the editor saves it.
/// </summary>
[TestSuite]
public class NetworkedSampleTests
{
	private const string LevelPath = "res://forge_samples/demos/05_networked_statescript/scenes/networked_level.tscn";

	private const string BrazierGrantsPath =
		"res://forge_samples/demos/05_networked_statescript/abilities/brazier_grants.tres";

	[TestCase]
	[RequireGodotRuntime]
	public void The_level_keeps_its_networked_brazier_through_a_save()
	{
		Node level = GD.Load<PackedScene>(LevelPath).Instantiate(PackedScene.GenEditState.Instance);
		var saved = new PackedScene();
		saved.Pack(level).Should().Be(Error.Ok);
		level.Free();

		Node reloaded = saved.Instantiate();

		try
		{
			reloaded.GetNode<ForgeEntity>("Stations/Brazier/Forge Entity").Networked.Should().BeTrue();
			reloaded.GetNode<ForgeEffect>("Stations/Brazier/Forge Entity/Grant Abilities").EffectData!.ResourcePath
				.Should().Be(BrazierGrantsPath);
		}
		finally
		{
			reloaded.Free();
		}
	}
}
