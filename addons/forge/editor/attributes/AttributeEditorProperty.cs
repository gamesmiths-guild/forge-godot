// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

/// <summary>
/// Inspector editor for a fully qualified <c>Set.Attribute</c> string property. The picker itself lives in the shared
/// <see cref="AttributeSelectionControl"/>, so the inspector and the statescript editors offer the same experience.
/// </summary>
[Tool]
public partial class AttributeEditorProperty : EditorProperty, ISerializationListener
{
	// Zero means "split each row evenly", matching how the inspector lays out its own properties.
	private const float LabelWidth = 0.0f;

	// The inspector builds nested resource boxes named sub_inspector_bg0 through sub_inspector_bg16, one per nesting
	// level, and stops deepening the tint past the last one.
	private const int MaxNestingLevel = 16;

	private AttributeSelectionControl? _selectionControl;
	private PanelContainer? _groupPanel;
	private StyleBoxFlat? _groupOutline;
	private AttributePropertyHeader? _header;

	/// <summary>
	/// Gets or sets a value indicating whether the picker offers a "None" entry, for properties where leaving the
	/// attribute unset is a valid configuration rather than a missing one. Set before the editor enters the tree.
	/// </summary>
	public bool AllowNone { get; set; }

	/// <inheritdoc/>
	public override void _Ready()
	{
		_selectionControl = new AttributeSelectionControl
		{
			AllowNone = AllowNone,
			LabelWidth = LabelWidth,
			UseInspectorStyling = true,
			SetLabel = "Attribute Set:",
			AttributeLabel = "Attribute:",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_selectionControl.ValueChanged += OnSelectionChanged;

		// The rows are wrapped so the group can carry the inspector's nested resource box as its own panel, which is
		// how the inspector itself styles a sub-resource: the box brings the tint for the nesting level and the content
		// margins that keep the border clear of the dropdowns. The property draws the border on top of that, so that it
		// encloses the property name as well.
		_groupPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_groupPanel.AddChild(_selectionControl);

		StyleBoxFlat? fill = CreateNestedResourceBox(outline: false);

		if (fill is not null)
		{
			_groupPanel.AddThemeStyleboxOverride("panel", fill);
		}

		_groupOutline = CreateNestedResourceBox(outline: true);
		Resized += QueueRedraw;

		AddChild(_groupPanel);

		// Added last so it sits behind nothing of its own, and painted behind the property's own drawing so the name
		// the engine writes on top of it stays visible.
		_header = new AttributePropertyHeader();
		_header.SetStyle(GetHeaderStyle());
		AddChild(_header);

		// The picker is two full-width rows, so it belongs under the property name rather than squeezed beside it.
		SetBottomEditor(_groupPanel);

		// EditorProperty fills the bottom editor's rect with its "child_bg" stylebox, which reads as a lighter band
		// behind the two rows. That stylebox cannot be overridden locally - the inspector copies its theme cache down
		// to every property - so the draw itself is switched off, letting the group's own box stand on its own.
		DrawBackground = false;
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		// The name row runs from the top of the property down to wherever the bottom editor was just placed, so the
		// band can only be sized once the engine has finished laying the children out.
		if (what == NotificationSortChildren
			&& _header is not null && IsInstanceValid(_header)
			&& _groupPanel is not null && IsInstanceValid(_groupPanel))
		{
			_header.SetHeaderSize(new Vector2(Size.X, _groupPanel.Position.Y));
		}
	}

	/// <inheritdoc/>
	public override void _Draw()
	{
		if (_groupOutline is null)
		{
			return;
		}

		// Drawn from the property because it is the only node spanning both the name row and the picker; the panel
		// below covers just the two dropdown rows.
		DrawStyleBox(_groupOutline, new Rect2(Vector2.Zero, Size));
	}

	/// <inheritdoc/>
	public override void _UpdateProperty()
	{
		if (_selectionControl is null || !IsInstanceValid(_selectionControl))
		{
			return;
		}

		string value = GetEditedObject().Get(GetEditedProperty()).AsString();

		// Picking a set clears the attribute and writes an empty key back, which lands here as an update. Reapplying it
		// would throw away the set the user just chose, so only push values that actually differ from what is shown.
		if (value == _selectionControl.AttributeKey)
		{
			return;
		}

		_selectionControl.SetValue(value);
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		ReleaseUiState();
		FreeAllChildren();
		base._ExitTree();
	}

	/// <inheritdoc/>
	public void OnBeforeSerialize()
	{
		ReleaseUiState();
		FreeAllChildren();
	}

	/// <inheritdoc/>
	public void OnAfterDeserialize()
	{
		// This method was intentionally left blank.
	}

	/// <summary>
	/// Takes the box the inspector draws around a nested resource, tinted for the depth this property sits at, so the
	/// group reads like the sub-resources it shares a panel with.
	/// </summary>
	/// <param name="outline">
	/// True for the border enclosing the whole property, false for the tinted body behind the picker rows. The two are
	/// split because only the property spans the name row, while only the panel can supply the padding.
	/// </param>
	/// <returns>The stylebox, or null when the editor theme has no nested resource box.</returns>
	private StyleBoxFlat? CreateNestedResourceBox(bool outline)
	{
		Theme editorTheme = EditorInterface.Singleton.GetEditorTheme();
		string styleboxName = $"sub_inspector_bg{GetNestingLevel()}";

		if (!editorTheme.HasStylebox(styleboxName, "EditorStyles")
			|| editorTheme.GetStylebox(styleboxName, "EditorStyles") is not StyleBoxFlat nestedBox)
		{
			return null;
		}

		var style = (StyleBoxFlat)nestedBox.Duplicate();

		if (outline)
		{
			// No center, so the property name the engine draws underneath stays readable.
			style.DrawCenter = false;
		}
		else
		{
			// The panel keeps its sides and bottom. It is a child, so it paints over the property's border wherever the
			// two overlap; drawing the same border itself is what keeps the box unbroken down the picker rows. Only the
			// top goes, since that edge falls between the name row and the rows below, where there is no box to close.
			style.BorderWidthTop = 0;
		}

		return style;
	}

	/// <summary>
	/// Takes the band the inspector paints behind a property whose sub-inspector is open - the accent header above a
	/// nested resource such as ScalableFloat - so the name row reads as this group's header.
	/// </summary>
	/// <returns>The stylebox, or null when the editor theme has no such band.</returns>
	private StyleBox? GetHeaderStyle()
	{
		Theme editorTheme = EditorInterface.Singleton.GetEditorTheme();
		string styleboxName = $"sub_inspector_property_bg{GetNestingLevel()}";

		return editorTheme.HasStylebox(styleboxName, "EditorStyles")
			? editorTheme.GetStylebox(styleboxName, "EditorStyles")
			: null;
	}

	/// <summary>
	/// Counts how deeply this property is nested, the way the inspector does when it picks a nested resource's color:
	/// by walking up the tree and counting the editor properties above it, this one included.
	/// </summary>
	/// <returns>The nesting level, clamped to the deepest box the theme provides.</returns>
	private int GetNestingLevel()
	{
		int level = 0;

		for (Node? node = this; node is not null; node = node.GetParent())
		{
			if (node is EditorProperty)
			{
				level++;

				if (level == MaxNestingLevel)
				{
					break;
				}
			}
		}

		return level;
	}

	private void OnSelectionChanged()
	{
		if (_selectionControl is null || !IsInstanceValid(_selectionControl))
		{
			return;
		}

		EmitChanged(GetEditedProperty(), _selectionControl.AttributeKey);
	}

	private void ReleaseUiState()
	{
		Resized -= QueueRedraw;

		if (_selectionControl is not null && IsInstanceValid(_selectionControl))
		{
			_selectionControl.ValueChanged -= OnSelectionChanged;
		}

		_selectionControl = null;
		_groupPanel = null;
		_groupOutline = null;
		_header = null;
	}

	private void FreeAllChildren()
	{
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			Node child = GetChild(i);
			RemoveChild(child);
			child.Free();
		}
	}
}
#endif
