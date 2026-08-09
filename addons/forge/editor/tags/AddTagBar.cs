// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// The always-visible row for adding a tag: a name field, where it goes, and the button.
/// </summary>
[Tool]
internal sealed partial class AddTagBar : HBoxContainer, ISerializationListener
{
	private LineEdit? _tagField;
	private SearchableOptionButton? _destinationPicker;
	private Button? _addButton;

	// Remembered by reference, not by index: reordering sources renumbers them, and holding the old number would
	// silently point new tags at a different file.
	private string? _selectedReference;

	/// <summary>
	/// Raised with the destination source index and the typed key when the user asks to add a tag.
	/// </summary>
	public event Action<int, string>? AddRequested;

	/// <summary>
	/// Gets or sets a value indicating whether a destination picker is offered at all.
	/// </summary>
	public bool SourcePickerVisible { get; set; } = true;

	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		AddChild(new Label { Text = "Tag Name:" });

		_tagField = new LineEdit
		{
			PlaceholderText = "parent.child",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "Use dots to nest tags, for example cooldown.skill.dash.",
		};

		AddChild(_tagField);

		_destinationPicker = new SearchableOptionButton
		{
			TooltipText = "Which source the new tag is declared in.",
		};

		AddChild(_destinationPicker);

		_addButton = new Button
		{
			Text = "Add Tag",
		};

		AddChild(_addButton);

		RefreshSources();

		_tagField.TextSubmitted += OnTagSubmitted;
		_addButton.Pressed += OnAddPressed;
		_destinationPicker.ItemSelected += OnDestinationSelected;
	}

	public override void _ExitTree()
	{
		ReleaseUiState();
		base._ExitTree();
	}

	public void OnBeforeSerialize()
	{
		ReleaseUiState();
	}

	public void OnAfterDeserialize()
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Rebuilds the destination list, keeping the current choice where it still exists.
	/// </summary>
	public void RefreshSources()
	{
		if (_destinationPicker is null || !IsInstanceValid(_destinationPicker))
		{
			return;
		}

		_destinationPicker.Clear();

		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;
		int selectedIndex = 0;

		for (int i = 0; i < sources.Count; i++)
		{
			_destinationPicker.AddItem(sources[i].DisplayName);

			if (sources[i].Reference == _selectedReference)
			{
				selectedIndex = i;
			}
		}

		if (sources.Count > 0)
		{
			_destinationPicker.Selected = selectedIndex;
			_selectedReference = sources[selectedIndex].Reference;
		}
		else
		{
			_selectedReference = null;
		}

		// With a single source there is nothing to choose, and the picker would just be noise next to the field.
		_destinationPicker.Visible = SourcePickerVisible && sources.Count > 1;
	}

	/// <summary>
	/// Points the row at a source and prefills the field, then puts the caret where typing should continue.
	/// </summary>
	/// <param name="sourceIndex">The destination source, or a negative value to leave it as it is.</param>
	/// <param name="prefill">The key to start from, such as a parent key plus a dot.</param>
	public void PrepareFor(int sourceIndex, string prefill)
	{
		if (_tagField is null || !IsInstanceValid(_tagField))
		{
			return;
		}

		if (sourceIndex >= 0 && _destinationPicker is not null && IsInstanceValid(_destinationPicker)
			&& sourceIndex < _destinationPicker.ItemCount)
		{
			_destinationPicker.Selected = sourceIndex;
			RememberSelection(sourceIndex);
		}

		_tagField.Text = prefill;
		_tagField.GrabFocus();
		_tagField.CaretColumn = prefill.Length;
	}

	/// <summary>
	/// Empties the field, after a tag was accepted.
	/// </summary>
	public void Clear()
	{
		if (_tagField is not null && IsInstanceValid(_tagField))
		{
			_tagField.Text = string.Empty;
		}
	}

	private void OnDestinationSelected(long index)
	{
		RememberSelection((int)index);
	}

	private void RememberSelection(int index)
	{
		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;

		_selectedReference = index >= 0 && index < sources.Count ? sources[index].Reference : null;
	}

	private void OnTagSubmitted(string text)
	{
		Submit();
	}

	private void OnAddPressed()
	{
		Submit();
	}

	private void Submit()
	{
		if (_tagField is null || !IsInstanceValid(_tagField) || string.IsNullOrWhiteSpace(_tagField.Text))
		{
			return;
		}

		int destination = _destinationPicker is not null && IsInstanceValid(_destinationPicker)
			? _destinationPicker.Selected
			: -1;

		AddRequested?.Invoke(destination, _tagField.Text);
	}

	private void ReleaseUiState()
	{
		if (_tagField is not null && IsInstanceValid(_tagField))
		{
			_tagField.TextSubmitted -= OnTagSubmitted;
		}

		if (_addButton is not null && IsInstanceValid(_addButton))
		{
			_addButton.Pressed -= OnAddPressed;
		}

		if (_destinationPicker is not null && IsInstanceValid(_destinationPicker))
		{
			_destinationPicker.ItemSelected -= OnDestinationSelected;
		}

		AddRequested = null;
		_tagField = null;
		_destinationPicker = null;
		_addButton = null;
	}
}
#endif
