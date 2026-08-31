// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Base editor for every resolver that produces a 2D query shape.
/// </summary>
/// <remarks>
/// Handles the two things they all share - reporting compatibility with the shape lane, and hosting the nested pickers
/// their dimensions are authored in - so a concrete shape editor only names its operands.
/// </remarks>
internal abstract partial class ShapeResolverEditorBase2D : NodeEditorProperty
{
	private const float LabelWidth = 70.0f;

	private readonly List<NestedResolverPicker> _pickers = [];

	private Action? _onChanged;

	/// <summary>
	/// Adds this shape's dimension rows.
	/// </summary>
	/// <param name="root">The container to add rows to.</param>
	/// <param name="existingResource">The resource being edited, when one exists.</param>
	protected abstract void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase2D? existingResource);

	/// <summary>
	/// Creates the resource for this shape with its own operands already applied.
	/// </summary>
	/// <returns>The resource to serialize.</returns>
	protected abstract ShapeResolverResourceBase2D BuildResource();

	/// <summary>
	/// Gets the graph being edited, for subclasses that host nested pickers.
	/// </summary>
	protected StatescriptGraph? Graph { get; private set; }

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(Shape2D);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		Graph = graph;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		BuildShapeRows(root, property?.Resolver as ShapeResolverResourceBase2D);
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = BuildResource();
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;

		foreach (NestedResolverPicker picker in _pickers)
		{
			picker.ClearCallbacks();
		}

		_pickers.Clear();
	}

	/// <summary>
	/// Builds a labelled row for a control that is not a nested picker.
	/// </summary>
	/// <param name="label">The row label.</param>
	/// <param name="editor">The control.</param>
	/// <returns>The row.</returns>
	protected static Control CreateRow(string label, Control editor)
	{
		return ResolverEditorLayoutUtilities.CreateLabeledRow(label, editor, LabelWidth);
	}

	/// <summary>
	/// Adds a nested picker for one of this shape's dimensions.
	/// </summary>
	/// <param name="root">The container to add the row to.</param>
	/// <param name="title">The row title.</param>
	/// <param name="existing">The stored operand, when there is one.</param>
	/// <param name="folded">Whether the row starts folded.</param>
	/// <param name="expectedTypes">The types the operand may produce.</param>
	/// <returns>The picker, so the caller can read it when saving.</returns>
	protected NestedResolverPicker AddDimensionRow(
		VBoxContainer root,
		string title,
		StatescriptResolverResource? existing,
		bool folded,
		Type[] expectedTypes)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			Graph!,
			existing,
			title,
			expectedTypes,
			isArray: false,
			folded,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);

		root.AddChild(picker);
		_pickers.Add(picker);
		return picker;
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
