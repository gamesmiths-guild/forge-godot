// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

/// <summary>
/// The shared attribute picker: a set dropdown followed by an attribute dropdown scoped to that set. Both the
/// inspector (<see cref="AttributeEditorProperty"/>) and the statescript editors build on this so attribute selection
/// looks and behaves the same everywhere, mirroring how the tag pickers share
/// <see cref="Tags.TagContainerSelectionControl"/>.
/// </summary>
/// <remarks>
/// The control never coerces the edited value. A value that is unset, or that names a set or attribute which no longer
/// exists, is surfaced as such and left untouched until the user picks something; it is not silently rewritten to the
/// first available option.
/// </remarks>
[Tool]
internal sealed partial class AttributeSelectionControl : VBoxContainer, ISerializationListener
{
	private const string NoneLabel = "None";
	private const string MissingSuffix = " (missing)";

	private const float InspectorSplitRatio = 0.5f;

	private OptionButton? _setDropdown;
	private OptionButton? _attributeDropdown;

	private Control? _setEditorSlot;
	private Control? _attributeEditorSlot;

	/// <summary>
	/// Raised whenever the user changes the selection. Not raised by <see cref="SetValue(string?, string?)"/>.
	/// </summary>
	public event Action? ValueChanged;

	/// <summary>
	/// Gets or sets a value indicating whether the picker offers a "None" entry, for properties where leaving the
	/// attribute unset is a valid configuration rather than a missing one. Set before the control enters the tree.
	/// </summary>
	/// <remarks>
	/// When false, "None" is still shown while the current value is unset or dangling — otherwise the picker would be
	/// claiming a selection that does not exist — but it disappears once a valid attribute is chosen.
	/// </remarks>
	public bool AllowNone { get; set; }

	/// <summary>
	/// Gets or sets the width reserved for the row labels, so the picker lines up with the rows around it. Set to zero
	/// to instead split each row evenly between label and dropdown, the way the Godot inspector lays out its own
	/// properties. Set before the control enters the tree.
	/// </summary>
	public float LabelWidth { get; set; } = 60.0f;

	/// <summary>
	/// Gets or sets a value indicating whether the dropdowns are built the way the inspector builds its own enum
	/// properties, so they sit alongside them without standing out. Set before the control enters the tree.
	/// </summary>
	public bool UseInspectorStyling { get; set; }

	/// <summary>
	/// Gets or sets the label for the attribute set row. Set before the control enters the tree.
	/// </summary>
	public string SetLabel { get; set; } = "Set:";

	/// <summary>
	/// Gets or sets the label for the attribute row. Defaults to the abbreviation that fits the narrow statescript
	/// rows; the inspector, which has room for it, spells the word out. Set before the control enters the tree.
	/// </summary>
	public string AttributeLabel { get; set; } = "Attr:";

	/// <summary>Gets the currently selected attribute set class name, or an empty string when unset.</summary>
	public string SetClass { get; private set; } = string.Empty;

	/// <summary>Gets the currently selected attribute name, or an empty string when unset.</summary>
	public string AttributeName { get; private set; } = string.Empty;

	/// <summary>
	/// Gets the selection as a fully qualified <c>Set.Attribute</c> key, or an empty string when either half is unset.
	/// </summary>
	public string AttributeKey => SetClass.Length == 0 || AttributeName.Length == 0
		? string.Empty
		: $"{SetClass}.{AttributeName}";

	/// <summary>
	/// Splits a fully qualified <c>Set.Attribute</c> key into its two halves.
	/// </summary>
	/// <param name="key">The key to split. May be empty.</param>
	/// <param name="setClass">Receives the attribute set class name, or an empty string.</param>
	/// <param name="attributeName">Receives the attribute name, or an empty string.</param>
	public static void ParseKey(string? key, out string setClass, out string attributeName)
	{
		setClass = string.Empty;
		attributeName = string.Empty;

		if (string.IsNullOrEmpty(key))
		{
			return;
		}

		// Attribute set class names carry no dots, so the first dot separates the set from the attribute name.
		int dot = key.IndexOf('.', StringComparison.Ordinal);

		if (dot <= 0 || dot >= key.Length - 1)
		{
			setClass = key;
			return;
		}

		setClass = key[..dot];
		attributeName = key[(dot + 1)..];
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		if (UseInspectorStyling)
		{
			ApplyInspectorRowSpacing();
		}

		_setDropdown = new SearchableOptionButton
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		Control setEditor = _setDropdown;

		if (UseInspectorStyling)
		{
			ApplyInspectorStyling(_setDropdown);
			setEditor = WrapWithInspectorBackground(_setDropdown);
		}

		_setEditorSlot = setEditor;
		_setDropdown.ItemSelected += OnSetSelected;
		_setDropdown.GetPopup().AboutToPopup += PopulateSetDropdown;
		AddChild(CreateLabeledRow(SetLabel, setEditor));

		_attributeDropdown = new SearchableOptionButton
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		Control attributeEditor = _attributeDropdown;

		if (UseInspectorStyling)
		{
			ApplyInspectorStyling(_attributeDropdown);
			attributeEditor = WrapWithInspectorBackground(_attributeDropdown);
		}

		_attributeEditorSlot = attributeEditor;
		_attributeDropdown.ItemSelected += OnAttributeSelected;
		_attributeDropdown.GetPopup().AboutToPopup += PopulateAttributeDropdown;
		AddChild(CreateLabeledRow(AttributeLabel, attributeEditor));

		if (UseInspectorStyling)
		{
			Resized += UpdateInspectorSplit;
			UpdateInspectorSplit();
		}

		PopulateSetDropdown();
		PopulateAttributeDropdown();
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		ReleaseUiState();
		base._ExitTree();
	}

	/// <inheritdoc/>
	public void OnBeforeSerialize()
	{
		// An assembly reload drops every delegate-backed signal connection, so they have to be released here, while
		// they still exist. Doing it from _ExitTree alone means the disconnect runs against connections Godot already
		// took away, which it reports as an error.
		ReleaseUiState();
	}

	/// <inheritdoc/>
	public void OnAfterDeserialize()
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Sets the displayed selection without raising <see cref="ValueChanged"/>.
	/// </summary>
	/// <param name="setClass">The attribute set class name, or null/empty for unset.</param>
	/// <param name="attributeName">The attribute name, or null/empty for unset.</param>
	public void SetValue(string? setClass, string? attributeName)
	{
		SetClass = setClass ?? string.Empty;
		AttributeName = attributeName ?? string.Empty;

		if (_setDropdown is not null)
		{
			PopulateSetDropdown();
			PopulateAttributeDropdown();
		}
	}

	/// <summary>
	/// Sets the displayed selection from a fully qualified <c>Set.Attribute</c> key, without raising
	/// <see cref="ValueChanged"/>.
	/// </summary>
	/// <param name="attributeKey">The key to display. May be empty.</param>
	public void SetValue(string? attributeKey)
	{
		ParseKey(attributeKey, out string setClass, out string attributeName);
		SetValue(setClass, attributeName);
	}

	/// <summary>
	/// Mirrors how <c>EditorPropertyEnum</c> builds its <see cref="OptionButton"/>. The flat flag alone strips the
	/// button background without putting anything back; it is the theme variation that supplies the inspector's own
	/// dropdown styling, so the two have to be applied together. Clipping keeps a long attribute name from widening
	/// the inspector.
	/// </summary>
	/// <param name="dropdown">The dropdown to style.</param>
	private static void ApplyInspectorStyling(OptionButton dropdown)
	{
		dropdown.Flat = true;
		dropdown.ThemeTypeVariation = "EditorInspectorButton";
		dropdown.ClipText = true;
		dropdown.FitToLongestItem = false;
	}

	/// <summary>
	/// Puts the inspector's own editor background behind a dropdown.
	/// </summary>
	/// <remarks>
	/// <c>EditorProperty</c> paints its <c>child_bg</c> stylebox behind whatever region holds the editor: the right
	/// half for an inline editor, the full width for a bottom editor. That is the box behind the inspector's own
	/// dropdowns, and it is also the full-width band a bottom editor would otherwise get, so the property switches the
	/// drawing off and each dropdown carries the box itself instead.
	/// </remarks>
	/// <param name="dropdown">The dropdown to wrap.</param>
	/// <returns>The control to place in the row, which is the dropdown itself when the stylebox is unavailable.
	/// </returns>
	private static Control WrapWithInspectorBackground(OptionButton dropdown)
	{
		Theme editorTheme = EditorInterface.Singleton.GetEditorTheme();

		if (!editorTheme.HasStylebox("child_bg", "EditorProperty"))
		{
			return dropdown;
		}

		// Godot draws the stylebox over the editor's rect rather than around it, so the content margins are dropped to
		// keep the box hugging the dropdown exactly as it does for an inline editor.
		var background = (StyleBox)editorTheme.GetStylebox("child_bg", "EditorProperty").Duplicate();
		background.ContentMarginLeft = 0;
		background.ContentMarginRight = 0;
		background.ContentMarginTop = 0;
		background.ContentMarginBottom = 0;

		var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		panel.AddThemeStyleboxOverride("panel", background);
		panel.AddChild(dropdown);

		return panel;
	}

	private static void AddItem(OptionButton dropdown, string label, string value)
	{
		dropdown.AddItem(label);
		dropdown.SetItemMetadata(dropdown.ItemCount - 1, value);
	}

	/// <summary>
	/// Matches the vertical rhythm the inspector gives its own properties: each property reserves a minimum height,
	/// and consecutive properties are spaced by the container's separation constant. Without this the two rows sit at
	/// whatever height the dropdowns happen to need, at the default container spacing, which reads as slightly off
	/// against the properties above and below.
	/// </summary>
	private void ApplyInspectorRowSpacing()
	{
		Theme editorTheme = EditorInterface.Singleton.GetEditorTheme();

		if (editorTheme.HasConstant("separation", "EditorPropertyContainer"))
		{
			AddThemeConstantOverride(
				"separation",
				editorTheme.GetConstant("separation", "EditorPropertyContainer"));
		}
	}

	/// <summary>
	/// Pins each row's editor half to the width the inspector would have given it: the row width times one minus the
	/// property split ratio, truncated, and right aligned. Recomputed on resize because it depends on the width.
	/// </summary>
	private void UpdateInspectorSplit()
	{
		// EditorProperty defaults to an even split with no fixed name width, and truncates the result to whole pixels.
		int editorWidth = (int)(Size.X * (1.0f - InspectorSplitRatio));

		if (_setEditorSlot is not null && IsInstanceValid(_setEditorSlot))
		{
			_setEditorSlot.CustomMinimumSize = new Vector2(editorWidth, _setEditorSlot.CustomMinimumSize.Y);
		}

		if (_attributeEditorSlot is not null && IsInstanceValid(_attributeEditorSlot))
		{
			_attributeEditorSlot.CustomMinimumSize =
				new Vector2(editorWidth, _attributeEditorSlot.CustomMinimumSize.Y);
		}
	}

	private HBoxContainer CreateLabeledRow(string labelText, Control editor)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		if (UseInspectorStyling)
		{
			Theme editorTheme = EditorInterface.Singleton.GetEditorTheme();

			if (editorTheme.HasConstant("inspector_property_height", "Editor"))
			{
				row.CustomMinimumSize = new Vector2(
					0,
					editorTheme.GetConstant("inspector_property_height", "Editor"));
			}

			// The inspector sizes an editor as the full width times one minus its split ratio, right aligned, and
			// takes the gap beside the label out of the label's own text area. A container separation would instead
			// shave half of itself off the dropdown, leaving it a pixel short of the editors above and below.
			row.AddThemeConstantOverride("separation", 0);
		}

		var label = new Label { Text = labelText };

		if (LabelWidth > 0.0f)
		{
			label.CustomMinimumSize = new Vector2(LabelWidth, 0);
			label.HorizontalAlignment = HorizontalAlignment.Right;
		}
		else if (UseInspectorStyling)
		{
			// The editor half is pinned by UpdateInspectorSplit rather than stretched: two equal stretch ratios split
			// an odd width by handing the leftover pixel to one of the children, which is not necessarily the half the
			// inspector would have given it.
			label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			editor.SizeFlagsHorizontal = SizeFlags.Fill;
		}
		else
		{
			// The inspector gives a property's name and its editor equal weight, so mirror that split here rather than
			// pinning the label to a fixed width and letting the dropdown swallow the rest of the row.
			label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			label.SizeFlagsStretchRatio = 1.0f;
			editor.SizeFlagsStretchRatio = 1.0f;
		}

		row.AddChild(label);
		row.AddChild(editor);
		return row;
	}

	private void PopulateSetDropdown()
	{
		if (_setDropdown is null || !IsInstanceValid(_setDropdown))
		{
			return;
		}

		Populate(_setDropdown, EditorUtils.GetAttributeSetOptions(), SetClass);
	}

	private void PopulateAttributeDropdown()
	{
		if (_attributeDropdown is null || !IsInstanceValid(_attributeDropdown))
		{
			return;
		}

		Populate(_attributeDropdown, EditorUtils.GetAttributeOptions(SetClass), AttributeName);

		// With no set chosen there is nothing to scope the attribute list to, so the second dropdown has no meaning
		// yet. Disabling it points the user at the set dropdown instead of showing an empty list.
		_attributeDropdown.Disabled = SetClass.Length == 0;
	}

	private void Populate(OptionButton dropdown, string[] options, string currentValue)
	{
		dropdown.Clear();

		int matchedIndex = -1;

		foreach (string option in options)
		{
			if (string.Equals(option, currentValue, StringComparison.OrdinalIgnoreCase))
			{
				matchedIndex = dropdown.ItemCount;
			}

			AddItem(dropdown, option, option);
		}

		if (currentValue.Length > 0 && matchedIndex < 0)
		{
			// A dangling reference: the stored value is kept and shown as broken rather than quietly replaced, so the
			// user can see what needs fixing.
			matchedIndex = dropdown.ItemCount;
			AddItem(dropdown, currentValue + MissingSuffix, currentValue);
		}

		if (AllowNone || matchedIndex < 0)
		{
			int noneIndex = dropdown.ItemCount;
			AddItem(dropdown, NoneLabel, string.Empty);

			if (matchedIndex < 0)
			{
				matchedIndex = noneIndex;
			}
		}

		dropdown.Selected = matchedIndex;
	}

	private void OnSetSelected(long index)
	{
		if (_setDropdown is null)
		{
			return;
		}

		string newSet = _setDropdown.GetItemMetadata((int)index).AsString();

		if (newSet == SetClass)
		{
			return;
		}

		SetClass = newSet;
		AttributeName = string.Empty;
		PopulateAttributeDropdown();
		ValueChanged?.Invoke();
	}

	private void OnAttributeSelected(long index)
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		string newAttribute = _attributeDropdown.GetItemMetadata((int)index).AsString();

		if (newAttribute == AttributeName)
		{
			return;
		}

		AttributeName = newAttribute;
		ValueChanged?.Invoke();
	}

	private void ReleaseUiState()
	{
		if (UseInspectorStyling)
		{
			Resized -= UpdateInspectorSplit;
		}

		if (_setDropdown is not null && IsInstanceValid(_setDropdown))
		{
			_setDropdown.ItemSelected -= OnSetSelected;
			_setDropdown.GetPopup().AboutToPopup -= PopulateSetDropdown;
		}

		if (_attributeDropdown is not null && IsInstanceValid(_attributeDropdown))
		{
			_attributeDropdown.ItemSelected -= OnAttributeSelected;
			_attributeDropdown.GetPopup().AboutToPopup -= PopulateAttributeDropdown;
		}

		ValueChanged = null;
		_setDropdown = null;
		_attributeDropdown = null;
		_setEditorSlot = null;
		_attributeEditorSlot = null;
	}
}
#endif
