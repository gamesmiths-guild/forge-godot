// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Editor.Cues;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Editor controls that leave the scene tree and come back, as they do when a dock moves or when a graph's tab is
/// switched away from and back to, driven in a live headless editor.
/// </summary>
internal static class ReattachTests
{
	private const string TagListenerNodeType = "Gamesmiths.Forge.Statescript.Nodes.State.TagListenerNode";

	/// <summary>
	/// Making the dock floating reparents it, which takes every control in it out of the tree and back. The tag
	/// container released its UI on the way out and never rebuilt it, since <c>_Ready</c> does not run again, so its
	/// button toggled without expanding anything. Closing and reopening the dock goes through the same reparent.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_tag_container_expands_after_the_dock_moves(EditorTestContext context)
	{
		context.OpenNewGraph();
		context.Dock.TestOnlyAddNode("Tag Listener", TagListenerNodeType);

		context.Dock.Close();
		context.Dock.Open();

		ExpandTagContainer(context.Dock).Should().BeTrue("the tag list has to open after the dock moved");
	}

	/// <summary>
	/// Switching tabs detaches the shown graph's node visuals and attaches them again on the way back, which broke the
	/// tag container the same way a dock move did.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_tag_container_expands_after_switching_back_to_its_tab(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		context.Dock.TestOnlyAddNode("Tag Listener", TagListenerNodeType);

		context.OpenNewGraph();
		context.Dock.OpenGraph(graph);

		ExpandTagContainer(context.Dock).Should().BeTrue("the tag list has to open after switching back to its tab");
	}

	/// <summary>
	/// The inspector keeps the property editors it already built when its dock moves, and Forge's freed their controls
	/// on the way out of the tree, so they came back empty.
	/// </summary>
	[EditorTest]
	public static void Inspector_properties_keep_their_controls_after_the_inspector_moves()
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
			(new ForgeSharedVariableSet(), typeof(SharedVariableSetEditorProperty)),
		];

		EditorInspector inspector = EditorInterface.Singleton.GetInspector();
		EditorDock inspectorDock = FindAncestor<EditorDock>(inspector);

		try
		{
			foreach ((GodotObject target, Type propertyType) in cases)
			{
				EditorInterface.Singleton.InspectObject(target);

				Node? property = FindDescendant(inspector, propertyType);
				property.Should().NotBeNull($"inspecting a {target.GetType().Name} has to show a {propertyType.Name}");
				int childCount = property!.GetChildCount();

				// Reopened as the selected tab: a hidden inspector defers building the next object's properties.
				inspectorDock.Close();
				inspectorDock.MakeVisible();

				GodotObject.IsInstanceValid(property).Should().BeTrue(
					"the inspector does not rebuild when its dock moves");
				property.GetChildCount().Should().Be(
					childCount, $"the {propertyType.Name} has to keep its controls after the Inspector moved");
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

	/// <summary>
	/// The Tags dock's add row unhooked its button and dropped its listeners on the way out of the tree, so after the
	/// dock was made floating, Add Tag did nothing. The bar is moved on its own here, since pressing Add in the real
	/// dock would write the tag into the project's source.
	/// </summary>
	[EditorTest]
	public static void An_add_tag_bar_still_submits_after_it_moves()
	{
		var bar = new AddTagBar();
		string? submitted = null;
		bar.AddRequested += (_, key) => submitted = key;

		Control host = EditorInterface.Singleton.GetBaseControl();
		host.AddChild(bar);

		try
		{
			host.RemoveChild(bar);
			host.AddChild(bar);

			bar.GetChildren().OfType<LineEdit>().Single().Text = "reattach.test";
			bar.GetChildren().OfType<Button>().Single(button => button is not OptionButton)
				.EmitSignal(BaseButton.SignalName.Pressed);

			submitted.Should().Be("reattach.test", "Add Tag has to reach its listener after the bar moved");
		}
		finally
		{
			bar.Free();
		}
	}

	private static bool ExpandTagContainer(Node dock)
	{
		TagContainerSelectionControl? tags = FindDescendant<TagContainerSelectionControl>(dock);
		tags.Should().NotBeNull("the Tag Listener's tag input has to show a tag container");

		Button toggle = tags!.GetChildren().OfType<Button>().Single();
		ScrollContainer list = tags.GetChildren().OfType<ScrollContainer>().Single();

		toggle.ButtonPressed = true;
		return list.Visible;
	}

	private static T FindAncestor<T>(Node node)
		where T : Node
	{
		for (Node? current = node.GetParent(); current is not null; current = current.GetParent())
		{
			if (current is T match)
			{
				return match;
			}
		}

		throw new InvalidOperationException($"'{node.Name}' has no {typeof(T).Name} ancestor.");
	}

	private static T? FindDescendant<T>(Node root)
		where T : Node
	{
		return (T?)FindDescendant(root, typeof(T));
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
