// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class ResolverEditorLayoutUtilities
{
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
