// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class ResolverEditorLayoutUtilities
{
	/// <summary>
	/// The label column width a resolver editor's setting rows share, so rows from different resolvers stacked in one
	/// node line their controls up at the same x.
	/// </summary>
	public const float SettingLabelWidth = 74.0f;

	/// <summary>
	/// Builds a labeled dropdown row for an enum setting and adds it to a container.
	/// </summary>
	/// <param name="root">The container to add the row to.</param>
	/// <param name="label">The row label.</param>
	/// <param name="itemNames">The entries, in enum order.</param>
	/// <param name="selectedIndex">The entry to start on, clamped to the entries that exist.</param>
	/// <param name="onChanged">Called when the selection changes.</param>
	/// <returns>The dropdown, so the caller can read its selection when saving.</returns>
	public static OptionButton CreateEnumRow(
		VBoxContainer root,
		string label,
		string[] itemNames,
		int selectedIndex,
		Action onChanged)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

		foreach (string itemName in itemNames)
		{
			dropdown.AddItem(itemName);
		}

		dropdown.Selected = Math.Clamp(selectedIndex, 0, itemNames.Length - 1);
		dropdown.ItemSelected += _ => onChanged();
		root.AddChild(CreateLabeledRow(label, dropdown, SettingLabelWidth));
		return dropdown;
	}

	public static HBoxContainer CreateLabeledRow(string labelText, Control editor, float labelWidth)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(labelWidth, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});
		row.AddChild(editor);
		return row;
	}

	public static HBoxContainer CreateIndentedRow(Control editor, float labelWidth)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			CustomMinimumSize = new Vector2(labelWidth, 0),
		});
		row.AddChild(editor);
		return row;
	}

	public static void RestoreSelection(OptionButton dropdown, IReadOnlyList<string> values, string selectedValue)
	{
		for (int i = 0; i < values.Count; i++)
		{
			if (values[i] == selectedValue)
			{
				dropdown.Selected = i;
				return;
			}
		}

		dropdown.Selected = 0;
	}
}
#endif
