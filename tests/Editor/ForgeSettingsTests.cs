// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// The project settings the plugin declares as it loads, checked in the one process that declares them.
/// </summary>
internal static class ForgeSettingsTests
{
	/// <summary>
	/// Every colour the physics debug drawing uses is a Color setting a project can edit, and until it is edited it
	/// reads back as the colour the drawer falls back to - so an untouched project draws as it always did, and the
	/// value the dialog resets to is the one it draws in.
	/// </summary>
	[EditorTest]
	public static void Every_debug_colour_is_declared_as_a_colour_setting()
	{
		foreach (PhysicsDebugColor color in PhysicsDebugDraw3D.Colors.Concat(PhysicsDebugDraw2D.Colors))
		{
			ProjectSettings.HasSetting(color.Setting)
				.Should().BeTrue($"{color.Setting} is declared when the plugin loads");

			Variant value = ProjectSettings.GetSetting(color.Setting);

			value.VariantType
				.Should().Be(Variant.Type.Color, $"{color.Setting} is edited with a colour picker");
			value.AsColor()
				.Should().Be(color.Default, $"{color.Setting} starts as the colour the drawer falls back to");
		}
	}
}
#endif
