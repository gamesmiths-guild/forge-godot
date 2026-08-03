// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Godot;
using VariableScope = Gamesmiths.Forge.Statescript.VariableScope;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Custom node editor for the <c>ForEachNode</c>. The Element output picks any non-array graph variable, value-typed
/// or object-backed, and that choice types the Array input row — the same rule the runtime applies when it decides
/// which lane to read the source through.
/// </summary>
[Tool]
internal sealed partial class ForEachNodeEditor : CustomNodeEditor
{
	private const string InputFoldKey = "_fold_input";
	private const string OutputFoldKey = "_fold_output";
	private const string ElementFoldKey = "_fold_output_element";

	private StatescriptVariableType? _elementVariableType;
	private string _elementObjectTypeId = string.Empty;

	[NonSerialized]
	private StatescriptNodeDiscovery.NodeTypeInfo? _cachedTypeInfo;

	[NonSerialized]
	private VBoxContainer? _cachedArrayContainer;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.State.ForEachNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		_cachedTypeInfo = typeInfo;
		ResolveElementType();

		BuildInputSection(typeInfo);
		BuildOutputSection(typeInfo);
	}

	/// <inheritdoc/>
	internal override void Unbind()
	{
		base.Unbind();
		_cachedTypeInfo = null;
		_cachedArrayContainer = null;
	}

	private static Variant? GetDefaultInputConstant(int inputIndex)
	{
		// The loop's condition means "keep going", so its conventional default is true. Left at the bool zero value a
		// freshly dropped node would be seeded with a constant false and silently run no iterations at all.
		return inputIndex == ForEachNode.ConditionInput ? true : null;
	}

	private void BuildInputSection(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.InputPropertiesInfo.Length == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			InputFoldKey,
			GetFoldState(InputFoldKey));

		// A FoldableContainer fits every child into the same rect, so all rows stack inside a single VBox child.
		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		// The array row lives in its own container so it can be rebuilt on its own when the element type changes.
		_cachedArrayContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		root.AddChild(_cachedArrayContainer);
		RebuildArrayRow();

		for (int i = 1; i < typeInfo.InputPropertiesInfo.Length; i++)
		{
			AddInputPropertyRow(
				typeInfo.InputPropertiesInfo[i],
				i,
				root,
				defaultConstantValue: GetDefaultInputConstant(i));
		}
	}

	private void BuildOutputSection(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.OutputVariablesInfo.Length == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Output Variables",
			OutputVariableColor,
			OutputFoldKey,
			GetFoldState(OutputFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		AddElementOutputRow(root, typeInfo.OutputVariablesInfo[ForEachNode.ElementOutput].Label);

		if (typeInfo.OutputVariablesInfo.Length > ForEachNode.IndexOutput)
		{
			AddScalarOutputVariableRow(
				root,
				typeInfo.OutputVariablesInfo[ForEachNode.IndexOutput].Label,
				ForEachNode.IndexOutput,
				typeInfo.OutputVariablesInfo[ForEachNode.IndexOutput].ValueType);
		}
	}

	private void AddElementOutputRow(VBoxContainer container, string label)
	{
		// Either lane can be iterated, so every non-array variable is a candidate; the one picked is what types the
		// source read.
		var candidates = new List<string>();

		foreach (StatescriptGraphVariable variable in Graph.Variables)
		{
			if (!variable.IsArray && !string.IsNullOrEmpty(variable.VariableName))
			{
				candidates.Add(variable.VariableName);
			}
		}

		string? current = FindBinding(StatescriptPropertyDirection.Output, ForEachNode.ElementOutput)?.Resolver
			is VariableResolverResource resolver
				? resolver.VariableName
				: null;

		// Drop a binding whose variable no longer exists rather than keeping it silently.
		if (!string.IsNullOrEmpty(current) && !candidates.Contains(current))
		{
			current = null;
			RemoveBinding(StatescriptPropertyDirection.Output, ForEachNode.ElementOutput);
			_elementVariableType = null;
			_elementObjectTypeId = string.Empty;
		}

		AddOutputVariableBadgeRow(
			container,
			label,
			ElementFoldKey,
			candidates,
			current,
			OnElementVariableSelected);
	}

	private void OnElementVariableSelected(string? variableName)
	{
		StatescriptVariableType? previousType = _elementVariableType;
		string previousObjectTypeId = _elementObjectTypeId;

		VariableResolverResource? newResolver = null;
		_elementVariableType = null;
		_elementObjectTypeId = string.Empty;

		if (!string.IsNullOrEmpty(variableName))
		{
			foreach (StatescriptGraphVariable variable in Graph.Variables)
			{
				if (variable.VariableName != variableName)
				{
					continue;
				}

				_elementVariableType = variable.VariableType;
				_elementObjectTypeId = variable.ObjectTypeId;

				newResolver = new VariableResolverResource
				{
					VariableName = variable.VariableName,
					Scope = VariableScope.Graph,
					VariableType = variable.VariableType,
					ObjectTypeId = variable.ObjectTypeId,
					IsArray = false,
				};

				break;
			}
		}

		ApplyOutputVariableBinding(ForEachNode.ElementOutput, newResolver, "Change Element Variable");

		if (previousType == _elementVariableType && previousObjectTypeId == _elementObjectTypeId)
		{
			return;
		}

		// The element type is what the array row is filtered by, so a stale binding of the old type has to go.
		RemoveBinding(StatescriptPropertyDirection.Input, ForEachNode.ArrayInput);
		ActiveResolverEditors.Remove(
			new PropertySlotKey(StatescriptPropertyDirection.Input, ForEachNode.ArrayInput));

		RebuildArrayRow();
		RaisePropertyBindingChanged();
		ResetSize();
	}

	private void RebuildArrayRow()
	{
		if (_cachedTypeInfo is null || _cachedArrayContainer is null)
		{
			return;
		}

		ClearContainer(_cachedArrayContainer);

		if (!TryGetElementClrType(out Type elementType))
		{
			var placeholder = new Label
			{
				Text = "Select element variable first",
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};

			placeholder.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.4f));
			_cachedArrayContainer.AddChild(placeholder);
			ResetSize();
			return;
		}

		AddInputPropertyRow(
			_cachedTypeInfo.InputPropertiesInfo[ForEachNode.ArrayInput] with
			{
				ExpectedType = elementType,
				IsArray = true,
			},
			ForEachNode.ArrayInput,
			_cachedArrayContainer);

		ResetSize();
	}

	private void ResolveElementType()
	{
		_elementVariableType = null;
		_elementObjectTypeId = string.Empty;

		if (FindBinding(StatescriptPropertyDirection.Output, ForEachNode.ElementOutput)?.Resolver
			is not VariableResolverResource resolver
			|| string.IsNullOrEmpty(resolver.VariableName))
		{
			return;
		}

		foreach (StatescriptGraphVariable variable in Graph.Variables)
		{
			if (variable.VariableName == resolver.VariableName)
			{
				_elementVariableType = variable.VariableType;
				_elementObjectTypeId = variable.ObjectTypeId;
				return;
			}
		}
	}

	private bool TryGetElementClrType(out Type clrType)
	{
		if (!string.IsNullOrEmpty(_elementObjectTypeId)
			&& StatescriptObjectVariableTypeRegistry.TryGet(
				_elementObjectTypeId,
				out StatescriptObjectVariableType? descriptor))
		{
			clrType = descriptor.ClrType;
			return true;
		}

		if (_elementVariableType is not null)
		{
			clrType = StatescriptVariableTypeConverter.ToSystemType(_elementVariableType.Value);
			return true;
		}

		clrType = null!;
		return false;
	}
}
#endif
