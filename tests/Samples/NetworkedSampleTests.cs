// Copyright © Gamesmiths Guild.

using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Example;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Components;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using GdUnit4;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Samples;

/// <summary>
/// Sample 05's level as the editor saves it, and what keeps it playing as the Statescript demo does.
/// </summary>
[TestSuite]
public class NetworkedSampleTests
{
	private const string LevelPath = "res://forge_samples/demos/05_networked_statescript/scenes/networked_level.tscn";

	private const string BrazierGrantsPath =
		"res://forge_samples/demos/05_networked_statescript/abilities/brazier_grants.tres";

	private const string Networked = "res://forge_samples/demos/05_networked_statescript/";

	private const string Statescript = "res://forge_samples/demos/03_statescript/";

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

	[TestCase("scenes/player.tscn", "scenes/player.tscn")]
	[TestCase("scenes/enemy.tscn", "scenes/enemy.tscn")]
	[RequireGodotRuntime]
	public void A_character_has_the_traits_and_attributes_the_Statescript_demo_s_has(string networked, string original)
	{
		Node ours = GD.Load<PackedScene>(Networked + networked).Instantiate();
		Node theirs = GD.Load<PackedScene>(Statescript + original).Instantiate();

		try
		{
			ForgeEntity entity = ours.GetNode<ForgeEntity>("Forge Entity");
			ForgeEntity reference = theirs.GetNode<ForgeEntity>("Forge Entity");

			entity.Networked.Should().BeTrue();
			entity.BaseTags.ContainerTags.Should().BeEquivalentTo(
				reference.BaseTags.ContainerTags,
				"water wets, fire burns and heals heal whoever has the trait for it");
			AttributeSets(entity).Should().Equal(AttributeSets(reference));
		}
		finally
		{
			ours.Free();
			theirs.Free();
		}
	}

	[TestCase("abilities/player_grants.tres", "abilities/player/grant_abilities.tres")]
	[TestCase("abilities/enemy_grants.tres", "abilities/enemy/grant_abilities.tres")]
	[RequireGodotRuntime]
	public void A_character_is_granted_the_abilities_the_Statescript_demo_s_is(string networked, string original)
	{
		GrantedAbilities(Networked + networked).Should().Equal(GrantedAbilities(Statescript + original));
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_shot_flies_as_far_and_as_fast_as_the_Statescript_demo_s_projectile()
	{
		ForgeProjectile3D projectile = GD.Load<PackedScene>(Statescript + "scenes/projectile.tscn")
			.Instantiate<ForgeProjectile3D>();
		StatescriptGraph flight = GD.Load<StatescriptGraph>(Networked + "graphs/projectile_flight.tres");
		StatescriptNode fly = flight.Nodes.Single(x => x.Title == "Move Body 3D");
		CharacterBody3D shot = GD.Load<PackedScene>(Networked + "scenes/projectile.tscn")
			.Instantiate<CharacterBody3D>();
		uint shotMask = shot.CollisionMask;
		shot.Free();
		CharacterBody3D player = GD.Load<PackedScene>(Networked + "scenes/player.tscn")
			.Instantiate<CharacterBody3D>();
		uint players = player.CollisionLayer;
		player.Free();

		try
		{
			var destination = (EntityTransformPoint3DResolverResource)fly.PropertyBindings[0].Resolver!;
			var speed = (VariantResolverResource)fly.PropertyBindings[1].Resolver!;

			speed.Value.AsSingle().Should().Be(projectile.Speed);
			((VariantResolverResource)destination.Offset!).Value.AsVector3()
				.Should().Be(new Vector3(0, 0, -projectile.MaxRange));
			shotMask.Should()
				.Be(projectile.CollisionMask | players, "the shot meets what the projectile meets, and players");
		}
		finally
		{
			projectile.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_shot_passes_through_the_player_who_fired_it()
	{
		CharacterBody3D player = GD.Load<PackedScene>(Networked + "scenes/player.tscn").Instantiate<CharacterBody3D>();
		ShotBody3D shot = GD.Load<PackedScene>(Networked + "scenes/projectile.tscn").Instantiate<ShotBody3D>();

		try
		{
			ForgeEntity shooter = player.GetNode<ForgeEntity>("Forge Entity");
			shot.OnInstantiated(shooter, shot.GetNode<ForgeEntity>("Forge Entity"));

			shot.GetCollisionExceptions().Should().ContainSingle().Which.Should().BeSameAs(player);
			GD.Load<StatescriptGraph>(Networked + "graphs/projectile.tres").Nodes
				.Single(x => x.Title == "Instantiate Scene 3D").CustomData["passOwnership"].AsBool()
				.Should().BeTrue("the shot learns who fired it as it's made");
		}
		finally
		{
			shot.Free();
			player.Free();
		}
	}

	private static string[] AttributeSets(ForgeEntity entity)
	{
		return [.. entity.GetChildren().OfType<ForgeAttributeSet>().Select(x => x.AttributeSetClass)];
	}

	private static string[] GrantedAbilities(string grants)
	{
		return [.. GD.Load<ForgeEffectData>(grants).Components!
			.OfType<GrantAbility>()
			.SelectMany(x => x.GrantAbilityConfigs)
			.Select(x => x.AbilityData!.Name)];
	}
}
