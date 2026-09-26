// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Tags;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Buttons whose handler needs its row's data, which they carry themselves and pass to a method connected by name, so
/// an assembly reload finds no lambda to copy. Each one has to act on its own row.
/// </summary>
internal static class ButtonHandlerTests
{
	/// <summary>
	/// The variables panel's rows: selecting a name, expanding an array, adding and removing an element, deleting.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Variable_panel_row_buttons_act_on_their_own_row(EditorTestContext context)
	{
		var graph = new StatescriptGraph { StatescriptName = "EditorTests" };
		graph.EnsureEntryNode();
		graph.Variables.Add(new StatescriptGraphVariable { VariableName = "first" });
		graph.Variables.Add(new StatescriptGraphVariable { VariableName = "list", IsArray = true });
		context.Dock.OpenGraph(graph);

		StatescriptVariablePanel panel = FindAll<StatescriptVariablePanel>(context.Dock).Single();

		CheckRowButtons(panel, graph.Variables[1].InitialArrayValues, "_array_elements", "_array_element_index");
		graph.Variables.Select(x => x.VariableName).Should().Equal(
			["list"], "deleting the first row removes its variable");
	}

	/// <summary>
	/// The same rows in a shared variable set's inspector.
	/// </summary>
	[EditorTest]
	public static void Shared_set_row_buttons_act_on_their_own_row()
	{
		var set = new ForgeSharedVariableSet();
		set.Variables.Add(new ForgeSharedVariableDefinition { VariableName = "first" });
		set.Variables.Add(new ForgeSharedVariableDefinition { VariableName = "list", IsArray = true });
		EditorInterface.Singleton.InspectObject(set);

		Node property = FindAll<SharedVariableSetEditorProperty>(EditorInterface.Singleton.GetInspector()).Single();

		CheckRowButtons(
			property,
			set.Variables[1].InitialArrayValues,
			"_shared_array_elements",
			"_shared_array_element_index");
		set.Variables.Select(x => x.VariableName).Should().Equal(
			["list"], "deleting the first row removes its variable");
	}

	/// <summary>
	/// A nested tag query's Remove button, which has to remove its own item and not another.
	/// </summary>
	[EditorTest]
	public static void A_nested_query_remove_button_removes_its_own_expression()
	{
		var query = new ForgeQueryExpression
		{
			ExpressionType = TagQueryExpressionType.AllExpressionsMatch,
			Expressions =
			[
				new ForgeQueryExpression { ExpressionType = TagQueryExpressionType.AnyTagsMatch },
				new ForgeQueryExpression { ExpressionType = TagQueryExpressionType.NoTagsMatch },
			],
		};

		EditorInterface.Singleton.InspectObject(query);

		QueryExpressionEditorControl editor =
			FindAll<QueryExpressionEditorControl>(EditorInterface.Singleton.GetInspector()).First();
		FindAll<Button>(editor).Last(x => x.Text == "Remove").EmitSignal(BaseButton.SignalName.Pressed);

		query.Expressions.Should().ContainSingle().Which.ExpressionType.Should().Be(
			TagQueryExpressionType.AnyTagsMatch, "the second item's Remove removes the second item");
	}

	/// <summary>
	/// The File menu's Save As and Load dialogs, which have to act on the file chosen in them.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Save_as_and_load_act_on_the_chosen_file(EditorTestContext context)
	{
		const string path = "user://forge_editor_tests_save_as.tres";

		try
		{
			context.OpenNewGraph();
			context.Dock.TestOnlyPressFileMenu(3);
			ChooseFile(context.Dock, path);

			context.Dock.CurrentGraph!.ResourcePath.Should().Be(path, "Save As opens the graph it saved");

			context.OpenNewGraph();
			context.Dock.TestOnlyPressFileMenu(1);
			ChooseFile(context.Dock, path);

			context.Dock.CurrentGraph!.ResourcePath.Should().Be(path, "Load opens the chosen file");
			context.Dock.CloseCurrentTab();
		}
		finally
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
		}
	}

	private static void CheckRowButtons(
		Node rows,
		GodotCollections.Array<Variant> listValues,
		string elementsKey,
		string elementKey)
	{
		NameButton(rows, "first").ButtonPressed = true;
		NameButton(rows, "list").ButtonPressed = true;
		NameButton(rows, "first").ButtonPressed.Should().BeFalse("selecting one variable deselects the other");

		Button arrayToggle =
			FindAll<Button>(rows).Single(x => x.Text.StartsWith("Array (size", StringComparison.Ordinal));
		arrayToggle.ButtonPressed = true;
		arrayToggle.GetMeta(elementsKey).As<VBoxContainer>().Visible.Should().BeTrue("expanding shows the elements");

		FindAll<Button>(rows).Single(x => x.TooltipText == "Add Element").EmitSignal(BaseButton.SignalName.Pressed);
		listValues.Should().ContainSingle("Add Element adds to its own array");

		FindAll<Button>(rows).Single(x => x.HasMeta(elementKey)).EmitSignal(BaseButton.SignalName.Pressed);
		listValues.Should().BeEmpty("the element's remove button removes it");

		FindAll<Button>(rows).First(x => x.TooltipText == "Remove Variable").EmitSignal(BaseButton.SignalName.Pressed);
	}

	private static Button NameButton(Node rows, string variableName)
	{
		return FindAll<Button>(rows).Single(x => x.ToggleMode && x.Text == variableName);
	}

	private static void ChooseFile(Node dock, string path)
	{
		EditorFileDialog dialog = dock.GetChildren().OfType<EditorFileDialog>().Last();
		dialog.EmitSignal(FileDialog.SignalName.FileSelected, path);

		// The dialog queued its own deletion, and the frame that would run it does not end inside a test.
		dialog.Free();
	}

	private static IEnumerable<T> FindAll<T>(Node root)
		where T : Node
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is T match)
			{
				yield return match;
			}

			foreach (T nested in FindAll<T>(child))
			{
				yield return nested;
			}
		}
	}
}
#endif
