// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

internal abstract partial class EffectApplicationNodeEditorBase : CustomNodeEditor
{
	private const string EffectIsArrayKey = "_effect_input_array";
	private const string TargetIsArrayKey = "_target_input_array";
	private const string InputFoldKey = "_fold_input";
	private const string OutputFoldKey = "_fold_output";
	private const string HandleOutputFoldKey = "_fold_output_handle";

	// Object variable type id (see ActiveEffectHandleObjectVariableType) the handle output binds to.
	private const string HandleObjectTypeId = "ActiveEffectHandle";

	[NonSerialized]
	private StatescriptNodeDiscovery.NodeTypeInfo? _cachedTypeInfo;

	[NonSerialized]
	private VBoxContainer? _inputEditorsContainer;

	private bool _effectIsArray;
	private bool _targetIsArray;

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		_cachedTypeInfo = typeInfo;

		_effectIsArray = ResolveInputShape(
			EffectIsArrayKey,
			FindBinding(StatescriptPropertyDirection.Input, 0));
		_targetIsArray = ResolveInputShape(
			TargetIsArrayKey,
			FindBinding(StatescriptPropertyDirection.Input, 1));

		FoldableContainer inputContainer = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			InputFoldKey,
			GetFoldState(InputFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		inputContainer.AddChild(root);

		_inputEditorsContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		root.AddChild(_inputEditorsContainer);

		RebuildInputEditors();
		BuildHandleOutputSection();
	}

	/// <inheritdoc/>
	internal override void Unbind()
	{
		base.Unbind();
		_cachedTypeInfo = null;
		_inputEditorsContainer = null;
	}

	private bool ResolveInputShape(string customDataKey, StatescriptNodeProperty? binding)
	{
		if (NodeResource.CustomData.TryGetValue(customDataKey, out Variant storedValue))
		{
			return storedValue.AsBool();
		}

		return binding?.Resolver switch
		{
			EffectResolverResource effectResolver => effectResolver.IsArray,
			VariableResolverResource variableResolver => variableResolver.IsArray,
			ArrayResolverResource => true,
			_ => false,
		};
	}

	private void RebuildInputEditors()
	{
		if (_cachedTypeInfo is null || _inputEditorsContainer is null || _cachedTypeInfo.InputPropertiesInfo.Length < 2)
		{
			return;
		}

		for (int i = 0; i < _cachedTypeInfo.InputPropertiesInfo.Length; i++)
		{
			ActiveResolverEditors.Remove(new PropertySlotKey(StatescriptPropertyDirection.Input, i));
		}

		ClearContainer(_inputEditorsContainer);

		StatescriptNodeDiscovery.InputPropertyInfo effectInfo = _cachedTypeInfo.InputPropertiesInfo[0];
		StatescriptNodeDiscovery.InputPropertyInfo targetInfo = _cachedTypeInfo.InputPropertiesInfo[1];

		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(
				effectInfo.Label,
				effectInfo.ExpectedType,
				_effectIsArray),
			0,
			_inputEditorsContainer,
			EffectIsArrayKey);
		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(
				targetInfo.Label,
				targetInfo.ExpectedType,
				_targetIsArray),
			1,
			_inputEditorsContainer,
			TargetIsArrayKey);

		// The optional context-data input (index 2) is scalar-only, so it has no single/array shape toggle.
		if (_cachedTypeInfo.InputPropertiesInfo.Length > 2)
		{
			StatescriptNodeDiscovery.InputPropertyInfo contextInfo = _cachedTypeInfo.InputPropertiesInfo[2];

			AddInputPropertyRow(
				new StatescriptNodeDiscovery.InputPropertyInfo(
					contextInfo.Label,
					contextInfo.ExpectedType,
					false),
				2,
				_inputEditorsContainer);
		}
	}

	private void BuildHandleOutputSection()
	{
		if (_cachedTypeInfo is null || _cachedTypeInfo.OutputVariablesInfo.Length == 0)
		{
			return;
		}

		FoldableContainer outputContainer = AddPropertySectionDivider(
			"Output Variables",
			OutputVariableColor,
			OutputFoldKey,
			GetFoldState(OutputFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		outputContainer.AddChild(root);

		// The output shape follows the inputs: an array of handles when either input is an array, otherwise a single
		// handle.
		bool outputIsArray = _effectIsArray || _targetIsArray;
		AddHandleOutputRow(root, _cachedTypeInfo.OutputVariablesInfo[0].Label, outputIsArray);
	}

	private void AddHandleOutputRow(VBoxContainer container, string label, bool isArray)
	{
		var candidates = new List<string>();

		foreach (StatescriptGraphVariable variable in Graph.Variables)
		{
			if (variable.ObjectTypeId == HandleObjectTypeId
				&& variable.IsArray == isArray
				&& !string.IsNullOrEmpty(variable.VariableName))
			{
				candidates.Add(variable.VariableName);
			}
		}

		string? current =
			FindBinding(StatescriptPropertyDirection.Output, 0)?.Resolver is VariableResolverResource resolver
				? resolver.VariableName
				: null;

		if (!string.IsNullOrEmpty(current) && !candidates.Contains(current))
		{
			current = null;
			RemoveBinding(StatescriptPropertyDirection.Output, 0);
		}

		AddOutputVariableBadgeRow(
			container,
			label,
			HandleOutputFoldKey,
			candidates,
			current,
			variableName => OnHandleOutputSelected(variableName, isArray));
	}

	private void OnHandleOutputSelected(string? variableName, bool isArray)
	{
		if (string.IsNullOrEmpty(variableName))
		{
			RemoveBinding(StatescriptPropertyDirection.Output, 0);
		}
		else
		{
			EnsureBinding(StatescriptPropertyDirection.Output, 0).Resolver = new VariableResolverResource
			{
				VariableName = variableName,
				Scope = VariableScope.Graph,
				ObjectTypeId = HandleObjectTypeId,
				IsArray = isArray,
			};
		}

		RaisePropertyBindingChanged();
		ResetSize();
	}
}
#endif
