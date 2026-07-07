// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Base editor for array-transformation resolvers (filter, sort, take, etc.). Authors the nested array source and lets
/// derived editors add their operation-specific rows (predicate, key selector, count, direction).
/// </summary>
[Tool]
internal abstract partial class ArrayTransformResolverEditorBase : NodeEditorProperty
{
	/// <summary>
	/// Adds the operation-specific rows below the source picker.
	/// </summary>
	/// <param name="root">The root container to add rows to.</param>
	/// <param name="graph">The current graph resource.</param>
	/// <param name="expectedType">The element type expected by the surrounding context.</param>
	/// <param name="onChanged">Callback invoked when the configuration changes.</param>
	protected abstract void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged);

	public override bool SupportsScalarValues => false;

	public override bool SupportsArrayValues => true;

	protected NestedResolverPicker? SourcePicker { get; private set; }

	protected ArrayTransformResolverResourceBase? ExistingResource { get; private set; }

	public override bool IsCompatibleWith(Type expectedType)
	{
		return ArrayResolverEditorUtilities.IsSupportedElementType(expectedType);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		ExistingResource = property?.Resolver as ArrayTransformResolverResourceBase;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		SourcePicker = new NestedResolverPicker();
		SourcePicker.Initialize(
			graph,
			ExistingResource?.Source,
			"Source:",
			GetSourceExpectedTypes(expectedType),
			isArray: true,
			ExistingResource?.SourceFolded ?? true,
			onChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(SourcePicker);

		BuildAdditionalRows(root, graph, expectedType, onChanged);
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = DisplayName;
		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (SourcePicker is not null && SourcePicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public override bool TryGetHighlightedSharedVariable(out string sharedVariableSetPath, out string variableName)
	{
		if (SourcePicker is not null
			&& SourcePicker.TryGetHighlightedSharedVariable(out sharedVariableSetPath, out variableName))
		{
			return true;
		}

		sharedVariableSetPath = string.Empty;
		variableName = string.Empty;
		return false;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		SourcePicker?.ClearCallbacks();
	}

	/// <summary>
	/// Gets the element types the source picker offers. Defaults to the context's expected element types; override for
	/// operations whose source is not constrained by the output element type (e.g. projections).
	/// </summary>
	/// <param name="expectedType">The element type expected by the surrounding context.</param>
	/// <returns>The allowed source element types.</returns>
	protected virtual Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return GetAllowedExpectedTypes(expectedType);
	}

	/// <summary>
	/// Creates and attaches a nested operand picker (predicate, key selector, count, or a second array).
	/// </summary>
	/// <param name="root">The container to add the picker to.</param>
	/// <param name="graph">The current graph resource.</param>
	/// <param name="existingResolver">The previously saved operand resource, when restoring.</param>
	/// <param name="title">The foldable title.</param>
	/// <param name="expectedTypes">The types the operand may resolve to.</param>
	/// <param name="folded">Whether the picker starts folded.</param>
	/// <param name="onChanged">Callback invoked when the operand changes.</param>
	/// <param name="isArray">Whether the operand is an array.</param>
	/// <param name="beginsIterationScope">Whether the operand is evaluated per element (a lambda), enabling the
	/// element resolvers in its subtree.</param>
	/// <returns>The attached picker.</returns>
	protected NestedResolverPicker AddOperandPicker(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptResolverResource? existingResolver,
		string title,
		Type[] expectedTypes,
		bool folded,
		Action onChanged,
		bool isArray = false,
		bool beginsIterationScope = false)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			graph,
			existingResolver,
			title,
			expectedTypes,
			isArray,
			folded,
			onChanged,
			RaiseLayoutSizeChanged,
			beginsIterationScope || IterationScope);
		root.AddChild(picker);
		return picker;
	}
}
#endif
