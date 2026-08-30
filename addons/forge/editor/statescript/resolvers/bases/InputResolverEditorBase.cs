// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Base editor for every resolver that reads input actions.
/// </summary>
/// <remarks>
/// They differ only in how many actions they name - one for a button, two for an axis, four for a vector - so the base
/// renders the rows and the subclass says which ones it wants and what to build from them.
/// </remarks>
internal abstract partial class InputResolverEditorBase : NodeEditorProperty
{
	private const float LabelWidth = 74.0f;

	private readonly List<InputActionNameField> _actionFields = [];

	private Action? _onChanged;

	/// <summary>
	/// Adds this resolver's rows. Called with the container everything goes into.
	/// </summary>
	/// <param name="root">The container to add rows to.</param>
	/// <param name="existingResolver">The resource being edited, when one exists.</param>
	protected abstract void BuildRows(VBoxContainer root, StatescriptResolverResource? existingResolver);

#pragma warning disable SA1202 // Elements should be ordered by access
	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		BuildRows(root, property?.Resolver);
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;

		foreach (InputActionNameField actionField in _actionFields)
		{
			actionField.ClearCallbacks();
		}

		_actionFields.Clear();
	}
#pragma warning restore SA1202 // Elements should be ordered by access

	/// <summary>
	/// Reads a field's text, tolerating a control freed by a hot reload.
	/// </summary>
	/// <param name="field">The field to read.</param>
	/// <returns>The text, or an empty string when the field is gone.</returns>
	protected static string ReadActionName(LineEdit? field)
	{
		return field is not null && IsInstanceValid(field) ? field.Text : string.Empty;
	}

	/// <summary>
	/// Adds a labeled action-name row backed by the Input Map dropdown.
	/// </summary>
	/// <param name="root">The container to add the row to.</param>
	/// <param name="label">The row label.</param>
	/// <param name="actionName">The name to start on.</param>
	/// <returns>The field, so the caller can read it when saving.</returns>
	protected LineEdit AddActionRow(VBoxContainer root, string label, string actionName)
	{
		var field = new LineEdit
		{
			Text = actionName,
			PlaceholderText = "action_name",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "An action from the project's Input Map.",
		};
		field.TextChanged += _ => NotifyChanged();

		var actionField = new InputActionNameField();
		actionField.Initialize(field, NotifyChanged);
		_actionFields.Add(actionField);

		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(label, actionField, LabelWidth));
		return field;
	}

	/// <summary>
	/// Adds a labeled dropdown row for an enum setting.
	/// </summary>
	/// <param name="root">The container to add the row to.</param>
	/// <param name="label">The row label.</param>
	/// <param name="itemNames">The entries, in enum order.</param>
	/// <param name="selectedIndex">The entry to start on.</param>
	/// <returns>The dropdown, so the caller can read its selection when saving.</returns>
	protected OptionButton AddEnumRow(VBoxContainer root, string label, string[] itemNames, int selectedIndex)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (string itemName in itemNames)
		{
			dropdown.AddItem(itemName);
		}

		dropdown.Selected = Math.Clamp(selectedIndex, 0, itemNames.Length - 1);
		dropdown.ItemSelected += _ => NotifyChanged();
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(label, dropdown, LabelWidth));
		return dropdown;
	}

	/// <summary>
	/// Runs the editor's change callback.
	/// </summary>
	protected void NotifyChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
