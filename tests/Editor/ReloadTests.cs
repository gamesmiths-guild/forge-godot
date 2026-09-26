// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Editor.Cues;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Editor controls that outlive an assembly reload. The reload itself cannot run inside the suite, so its first step on
/// each control, <see cref="ISerializationListener.OnBeforeSerialize"/>, is called directly.
/// </summary>
internal static class ReloadTests
{
	/// <summary>
	/// An Inspector hidden behind another dock tab keeps its property editors through a reload and goes on laying them
	/// out. Forge's freed all of their children first, among them containers the engine keeps raw pointers to, so that
	/// layout crashed the editor.
	/// </summary>
	[EditorTest]
	public static void Inspector_properties_keep_their_controls_through_a_reload()
	{
		var attributeSet = new ForgeAttributeSet();
		var cueHandler = new AudioCueHandler();

		(GodotObject Target, Type PropertyType)[] cases =
		[
			(new ForgeTagContainer(), typeof(TagContainerEditorProperty)),
			(new ForgeTag { Tag = string.Empty }, typeof(TagEditorProperty)),
			(new ForgeTagsSource(), typeof(TagsSourceEditorProperty)),
			(cueHandler, typeof(CueKeyEditorProperty)),
			(new ForgeModifier(), typeof(AttributeEditorProperty)),
			(attributeSet, typeof(AttributeSetClassEditorProperty)),
		];

		EditorInspector inspector = EditorInterface.Singleton.GetInspector();

		try
		{
			foreach ((GodotObject target, Type propertyType) in cases)
			{
				EditorInterface.Singleton.InspectObject(target);

				Node? property = FindDescendant(inspector, propertyType);
				property.Should().NotBeNull($"inspecting a {target.GetType().Name} has to show a {propertyType.Name}");
				int childCount = property!.GetChildCount();

				((ISerializationListener)property).OnBeforeSerialize();

				property.GetChildCount().Should().Be(
					childCount, $"the {propertyType.Name} has to keep its controls through a reload");
			}
		}
		finally
		{
			// Inspected nodes cannot be freed while the inspector is still showing them.
			EditorInterface.Singleton.InspectObject(new ForgeTag { Tag = string.Empty });
			attributeSet.Free();
			cueHandler.Free();
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
