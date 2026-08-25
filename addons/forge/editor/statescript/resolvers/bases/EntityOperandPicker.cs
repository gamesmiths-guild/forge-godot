// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Reusable control that authors a single "which entity" operand.
/// </summary>
/// <remarks>
/// <para>This is a <see cref="NestedResolverPicker"/> over the entity lane, so the operand offers every registered
/// resolver that produces an entity - the ability's own owner, source and target, a variable, the iterated element, an
/// entity named by a scene path, the first or a random one of an overlap query's results - and picks up anything added
/// later without this control being touched.</para>
/// <para>It replaced a fixed list of five entries whose sub-editor row could only render one thing, a variable. That
/// list could not be extended without teaching the row to render arbitrary editors, which is most of what a nested
/// picker already is.</para>
/// </remarks>
[Tool]
internal sealed partial class EntityOperandPicker : VBoxContainer
{
	private static readonly Type[] _entityExpectedTypes = [typeof(IForgeEntity)];

	private Action? _onChanged;
	private Action? _layoutSizeChanged;
	private NestedResolverPicker? _picker;
	private CheckBox? _noneCheckBox;

	/// <summary>
	/// Gets a value indicating whether the picker's operand section is folded.
	/// </summary>
	public bool Folded => _picker?.Folded ?? true;

	/// <summary>
	/// Initializes the entity operand picker.
	/// </summary>
	/// <param name="graph">The graph the operand is authored against.</param>
	/// <param name="existingResolver">The stored operand, or <see langword="null"/> for a fresh one.</param>
	/// <param name="label">The row's display label.</param>
	/// <param name="onChanged">Invoked whenever the authored operand changes.</param>
	/// <param name="layoutSizeChanged">Invoked when the row's height changes.</param>
	/// <param name="iterationScope">Whether the iterated element is in scope (inside an array operation's lambda).
	/// </param>
	/// <param name="allowNone">Whether the operand may be left empty. Pass <see langword="true"/> only where an absent
	/// entity is a meaningful state distinct from every selectable entity — a source used to filter a lookup, where
	/// absent means "match any source". It renders as a checkbox above the operand, and makes
	/// <see cref="BuildResource"/> return <see langword="null"/>.</param>
	/// <param name="folded">Whether the operand section starts folded.</param>
	public void Initialize(
		StatescriptGraph graph,
		StatescriptResolverResource? existingResolver,
		string label,
		Action onChanged,
		Action layoutSizeChanged,
		bool iterationScope = false,
		bool allowNone = false,
		bool folded = false)
	{
		_onChanged = onChanged;
		_layoutSizeChanged = layoutSizeChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		if (allowNone)
		{
			_noneCheckBox = new CheckBox
			{
				Text = $"Any {label.TrimEnd(':').ToLowerInvariant()}",
				ButtonPressed = existingResolver is null,
				TooltipText = "Leave the operand empty, which matches anything rather than naming one entity.",
			};

			_noneCheckBox.Toggled += _ => OnNoneToggled();
			AddChild(_noneCheckBox);
		}

		_picker = new NestedResolverPicker();
		_picker.Initialize(
			graph,
			existingResolver ?? (allowNone ? null : new AbilityOwnerResolverResource()),
			label,
			_entityExpectedTypes,
			isArray: false,
			folded,
			onChanged,
			layoutSizeChanged,
			iterationScope);

		AddChild(_picker);
		UpdatePickerVisibility();
	}

	/// <summary>
	/// Builds the authored operand, or <see langword="null"/> when the picker allows None and rests on it.
	/// </summary>
	/// <returns>The resolver resource, or <see langword="null"/> for None.</returns>
	public StatescriptResolverResource? BuildResource()
	{
		if (_noneCheckBox is not null && IsInstanceValid(_noneCheckBox) && _noneCheckBox.ButtonPressed)
		{
			return null;
		}

		return _picker?.BuildResource();
	}

	public void ClearCallbacks()
	{
		_onChanged = null;
		_layoutSizeChanged = null;
		_picker?.ClearCallbacks();
		_picker = null;
		_noneCheckBox = null;
	}

	/// <summary>
	/// Tries to provide the graph-variable name the operand reads, for highlight propagation.
	/// </summary>
	/// <param name="variableName">The variable name, when available.</param>
	/// <returns><see langword="true"/> when the operand reads a variable.</returns>
	public bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_picker is not null && _picker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	private void OnNoneToggled()
	{
		UpdatePickerVisibility();
		_onChanged?.Invoke();
		_layoutSizeChanged?.Invoke();
	}

	private void UpdatePickerVisibility()
	{
		if (_picker is not null && IsInstanceValid(_picker))
		{
			_picker.Visible = _noneCheckBox?.ButtonPressed != true;
		}
	}
}
#endif
