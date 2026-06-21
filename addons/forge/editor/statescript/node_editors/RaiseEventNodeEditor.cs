// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for the <c>RaiseEventNode</c>. Renders the event-tag input (a multi-tag picker, no shape toggle), the
/// target input (with a single/array shape toggle), and the remaining scalar inputs (source / magnitude / payload) as
/// standard rows.
/// </summary>
[Tool]
internal sealed partial class RaiseEventNodeEditor : CustomNodeEditor
{
	private const string TargetIsArrayKey = "_target_input_array";
	private const string InputFoldKey = "_fold_input";

	[NonSerialized]
	private StatescriptNodeDiscovery.NodeTypeInfo? _cachedTypeInfo;

	[NonSerialized]
	private VBoxContainer? _inputEditorsContainer;

	private bool _targetIsArray;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Action.RaiseEventNode";

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

		StatescriptNodeDiscovery.InputPropertyInfo eventTagInfo = _cachedTypeInfo.InputPropertiesInfo[0];
		StatescriptNodeDiscovery.InputPropertyInfo targetInfo = _cachedTypeInfo.InputPropertiesInfo[1];

		// Event tags are authored as a multi-tag set, so this row has no single/array shape toggle.
		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(eventTagInfo.Label, eventTagInfo.ExpectedType, false),
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

		// Remaining inputs (source / magnitude / payload) are optional scalar values.
		for (int i = 2; i < _cachedTypeInfo.InputPropertiesInfo.Length; i++)
		{
			StatescriptNodeDiscovery.InputPropertyInfo info = _cachedTypeInfo.InputPropertiesInfo[i];

			// Default the payload input (an EventPayloadRaiser) to the event payload provider editor.
			string? preferred = info.ExpectedType == typeof(EventPayloadRaiser) ? "EventPayload" : null;

			AddInputPropertyRow(
				new StatescriptNodeDiscovery.InputPropertyInfo(info.Label, info.ExpectedType, false),
				i,
				_inputEditorsContainer,
				preferredDefaultResolverTypeId: preferred);
		}
	}
}
#endif
