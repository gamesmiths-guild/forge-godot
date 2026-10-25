// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// The inspector of the character movement components, driven in a live headless editor.
/// </summary>
internal static class CharacterMovementInspectorTests
{
	/// <summary>
	/// The components are tool scripts, so in the editor their speed attribute gets the attribute picker and their move
	/// actions suggest the project's input actions. Neither happens for a script that isn't a tool.
	/// </summary>
	[EditorTest]
	public static void The_speed_attribute_has_a_picker_and_the_move_actions_have_suggestions()
	{
		Node[] components = [new ForgeCharacterMovement3D(), new ForgeCharacterMovement2D()];
		EditorInspector inspector = EditorInterface.Singleton.GetInspector();

		try
		{
			foreach (Node component in components)
			{
				EditorInterface.Singleton.InspectObject(component);

				FindDescendant(inspector, typeof(AttributeEditorProperty)).Should().NotBeNull(
					$"{component.GetType().Name} picks its speed attribute from the project's attribute sets");
				component.GetPropertyList()
					.Single(x => x["name"].AsString() == "MoveLeft")["hint"].AsInt32()
					.Should().Be((int)PropertyHint.EnumSuggestion);
			}
		}
		finally
		{
			// Inspected nodes cannot be freed while the inspector is still showing them.
			EditorInterface.Singleton.InspectObject(new ForgeTag { Tag = string.Empty });

			foreach (Node component in components)
			{
				component.Free();
			}
		}
	}

	private static Node? FindDescendant(Node root, Type type)
	{
		foreach (Node child in root.GetChildren())
		{
			if (type.IsInstanceOfType(child))
			{
				return child;
			}

			Node? nested = FindDescendant(child, type);

			if (nested is not null)
			{
				return nested;
			}
		}

		return null;
	}
}
#endif
