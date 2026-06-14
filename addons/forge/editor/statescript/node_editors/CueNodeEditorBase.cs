// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Shared node editor for the cue nodes. Renders the cue-tag input (a multi-tag picker, no shape toggle), the target
/// input (with a single/array shape toggle), and any remaining scalar inputs (magnitude / normalized magnitude /
/// source / custom parameters) as standard rows.
/// </summary>
internal abstract partial class CueNodeEditorBase : CustomNodeEditor
{
	private const string TargetIsArrayKey = "_target_input_array";
	private const string InputFoldKey = "_fold_input";

	[NonSerialized]
	private StatescriptNodeDiscovery.NodeTypeInfo? _cachedTypeInfo;

	[NonSerialized]
	private VBoxContainer? _inputEditorsContainer;

	private bool _targetIsArray;

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		_cachedTypeInfo = typeInfo;

		_targetIsArray = ResolveTargetShape(FindBinding(StatescriptPropertyDirection.Input, 1));

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

	private bool ResolveTargetShape(StatescriptNodeProperty? binding)
	{
		if (NodeResource.CustomData.TryGetValue(TargetIsArrayKey, out Variant storedValue))
		{
			return storedValue.AsBool();
		}

		return binding?.Resolver switch
		{
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

		StatescriptNodeDiscovery.InputPropertyInfo cueTagInfo = _cachedTypeInfo.InputPropertiesInfo[0];
		StatescriptNodeDiscovery.InputPropertyInfo targetInfo = _cachedTypeInfo.InputPropertiesInfo[1];

		// Cue tags are authored as a multi-tag set, so this row has no single/array shape toggle.
		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(cueTagInfo.Label, cueTagInfo.ExpectedType, false),
			0,
			_inputEditorsContainer);

		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(
				targetInfo.Label,
				targetInfo.ExpectedType,
				_targetIsArray),
			1,
			_inputEditorsContainer,
			TargetIsArrayKey);

		// Remaining inputs (magnitude / normalized magnitude / source / custom parameters) are optional scalar values.
		for (int i = 2; i < _cachedTypeInfo.InputPropertiesInfo.Length; i++)
		{
			StatescriptNodeDiscovery.InputPropertyInfo info = _cachedTypeInfo.InputPropertiesInfo[i];

			AddInputPropertyRow(
				new StatescriptNodeDiscovery.InputPropertyInfo(info.Label, info.ExpectedType, false),
				i,
				_inputEditorsContainer);
		}
	}
}
#endif
