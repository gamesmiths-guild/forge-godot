// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Base editor for array-operation resolvers that reduce an array to a scalar (count, any, sum, element access, etc.).
/// Authors the nested array source and lets derived editors add their operation-specific rows.
/// </summary>
[Tool]
internal abstract partial class ArrayReductionResolverEditorBase : NodeEditorProperty
{
	/// <summary>
	/// Gets the previously saved source resource from the restored property, when available.
	/// </summary>
	/// <param name="property">The property binding being restored, or <see langword="null"/>.</param>
	/// <returns>The saved source resource, or <see langword="null"/>.</returns>
	protected abstract StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property);

	/// <summary>
	/// Gets the previously saved source fold state from the restored property.
	/// </summary>
	/// <param name="property">The property binding being restored, or <see langword="null"/>.</param>
	/// <returns>Whether the source picker starts folded.</returns>
	protected abstract bool GetExistingSourceFolded(StatescriptNodeProperty? property);

	/// <summary>
	/// Adds the operation-specific rows below the source picker.
	/// </summary>
	/// <param name="root">The root container to add rows to.</param>
	/// <param name="graph">The current graph resource.</param>
	/// <param name="property">The property binding being restored, or <see langword="null"/>.</param>
	/// <param name="expectedType">The type expected by the surrounding context.</param>
	/// <param name="onChanged">Callback invoked when the configuration changes.</param>
	protected abstract void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged);

	protected NestedResolverPicker? SourcePicker { get; private set; }

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		SourcePicker = new NestedResolverPicker();
		SourcePicker.Initialize(
			graph,
			GetExistingSource(property),
			"Source:",
			GetSourceExpectedTypes(expectedType),
			isArray: true,
			GetExistingSourceFolded(property),
			onChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(SourcePicker);

		BuildAdditionalRows(root, graph, property, expectedType, onChanged);
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
	/// Gets the element types the source picker offers. Defaults to any array (value or entity); override to constrain
	/// (e.g. numeric-only aggregates, entity-only access).
	/// </summary>
	/// <param name="expectedType">The type expected by the surrounding context.</param>
	/// <returns>The allowed source element types.</returns>
	protected virtual Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return [typeof(ForgeVariant128), typeof(IForgeEntity)];
	}

	/// <summary>
	/// Creates and attaches a nested operand picker (predicate, index, value, etc.).
	/// </summary>
	/// <param name="root">The container to add the picker to.</param>
	/// <param name="graph">The current graph resource.</param>
	/// <param name="existingResolver">The previously saved operand resource, when restoring.</param>
	/// <param name="title">The foldable title.</param>
	/// <param name="expectedTypes">The types the operand may resolve to.</param>
	/// <param name="folded">Whether the picker starts folded.</param>
	/// <param name="onChanged">Callback invoked when the operand changes.</param>
	/// <param name="beginsIterationScope">Whether the operand is evaluated per element (a lambda), enabling the element
	/// resolvers in its subtree.</param>
	/// <returns>The attached picker.</returns>
	protected NestedResolverPicker AddOperandPicker(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptResolverResource? existingResolver,
		string title,
		Type[] expectedTypes,
		bool folded,
		Action onChanged,
		bool beginsIterationScope = false)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			graph,
			existingResolver,
			title,
			expectedTypes,
			isArray: false,
			folded,
			onChanged,
			RaiseLayoutSizeChanged,
			beginsIterationScope || IterationScope);
		root.AddChild(picker);
		return picker;
	}
}
#endif
