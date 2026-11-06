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

		NetworkManifestGenerator.InheritedScenePath(Derived).Should().Be("res://tests/Nodes/networked_body.tscn");
	}

	private static string Text(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		return file.GetAsText();
	}
}
#endif
