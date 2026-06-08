// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

internal abstract partial class EffectApplicationNodeEditorBase : CustomNodeEditor
{
	private const string EffectIsArrayKey = "_effect_input_array";
	private const string TargetIsArrayKey = "_target_input_array";
	private const string InputFoldKey = "_fold_input";

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
	}
}
#endif
