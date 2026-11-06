// Copyright © Gamesmiths Guild.

#if TOOLS
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Networking;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// The network manifest lists the scenes networked entities spawn from: those whose own ForgeEntity is networked.
/// </summary>
internal static class NetworkManifestTests
{
	/// <summary>
	/// A scene whose own ForgeEntity is networked is listed; one whose entity isn't, and a level that only turns it on
	/// for an instance it places, aren't.
	/// </summary>
	[EditorTest]
	public static void Scenes_with_a_networked_entity_of_their_own_are_listed()
	{
		NetworkManifestGenerator.IsNetworkedSceneText(Text("res://tests/Nodes/networked_body.tscn"))
			.Should().BeTrue();
		NetworkManifestGenerator.IsNetworkedSceneText(Text("res://forge_samples/demos/04_rewind/scenes/player.tscn"))
			.Should().BeFalse();

		const string Level = """
			[gd_scene format=3]

			[ext_resource type="PackedScene" path="res://tests/Nodes/networked_body.tscn" id="1_body"]

			[node name="Level" type="Node3D"]

			[node name="Body" parent="." instance=ExtResource("1_body")]

			[node name="Forge Entity" parent="Body"]
			Networked = true

			[editable path="Body"]
			""";

		NetworkManifestGenerator.IsNetworkedSceneText(Level)
			.Should().BeFalse("a level places, the server doesn't spawn it");
		NetworkManifestGenerator.InheritedScenePath(Level)
			.Should().BeNull("an instance below the root isn't what the scene inherits");

		const string Derived = """
			[gd_scene format=3]

			[ext_resource type="PackedScene" uid="uid://x" path="res://tests/Nodes/networked_body.tscn" id="1_body"]

			[node name="Derived Body" instance=ExtResource("1_body")]
			""";

		NetworkManifestGenerator.InheritedScenePath(Derived)
			.Should().Be("res://tests/Nodes/networked_body.tscn");

		const string Subclass = """
			[gd_scene format=3]

			[ext_resource type="Script" path="res://game/Hero.cs" id="1_hero"]

			[node name="Hero" type="Node"]
			script = ExtResource("1_hero")
			Networked = true
			""";

		NetworkManifestGenerator.IsNetworkedSceneText(Subclass)
			.Should().BeTrue("a class deriving from ForgeEntity counts");
	}

	/// <summary>
	/// A scene inherits being networked from the scene it inherits, unless it overrides it, either way.
	/// </summary>
	[EditorTest]
	public static void Inheriting_scenes_follow_the_scene_they_inherit_unless_they_say_otherwise()
	{
		Listed("res://tests/Nodes/networked_body.tscn", string.Empty).Should().BeTrue();
		Listed("res://tests/Nodes/networked_body.tscn", "Networked = false").Should().BeFalse();
		Listed("res://forge_samples/demos/04_rewind/scenes/player.tscn", string.Empty).Should().BeFalse();
		Listed("res://forge_samples/demos/04_rewind/scenes/player.tscn", "Networked = true").Should().BeTrue();
	}

	private static bool Listed(string inherited, string overrideLine)
	{
		const string Path = "user://forge_inheriting_scene_test.tscn";

		using (var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write))
		{
			file.StoreString(
				"[gd_scene format=3]\n\n" +
				$"[ext_resource type=\"PackedScene\" path=\"{inherited}\" id=\"1_base\"]\n\n" +
				"[node name=\"Derived\" instance=ExtResource(\"1_base\")]\n\n" +
				$"[node name=\"Forge Entity\" parent=\".\"]\n{overrideLine}\n");
		}

		bool listed = NetworkManifestGenerator.IsNetworkedScene(Path);
		DirAccess.RemoveAbsolute(Path);
		return listed;
	}

	private static string Text(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		return file.GetAsText();
	}
}
#endif
