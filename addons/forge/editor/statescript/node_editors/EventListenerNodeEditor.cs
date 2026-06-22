// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for the <c>EventListenerNode</c>. Renders the event-tag and listen-on inputs, then an Output Variables
/// section with the built-in Source/Target (entity) and Magnitude (float) outputs plus a payload provider whose
/// declared outputs each bind to a graph variable of the matching type. Variable dropdowns are filtered by the output's
/// type, and the default editor cannot bind object-lane outputs, so this node needs its own editor.
/// </summary>
[Tool]
internal sealed partial class EventListenerNodeEditor : CustomNodeEditor
{
	private const string InputFoldKey = "_fold_input";
	private const string OutputFoldKey = "_fold_output";
	private const string PayloadFoldKey = "_fold_payload_provider";

	// EventListenerNode.PayloadOutputInput — the input slot that stores the payload provider and its output bindings.
	private const int PayloadInputIndex = 2;

	private readonly List<string> _payloadProviderClassNames = [];
	private readonly System.Collections.Generic.Dictionary<string, string> _payloadVariableByOutput = [];

	[NonSerialized]
	private FoldableContainer? _payloadFoldable;

	[NonSerialized]
	private OptionButton? _payloadProviderDropdown;

	[NonSerialized]
	private VBoxContainer? _payloadOutputsContainer;

	private string _selectedPayloadProvider = string.Empty;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.State.EventListenerNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		FoldableContainer inputContainer = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			InputFoldKey,
			GetFoldState(InputFoldKey));

		var inputRoot = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		inputContainer.AddChild(inputRoot);

		// Only Event Tags and Listen On are authored as inputs; the payload slot is rendered in the output section
		// below.
		for (int i = 0; i < typeInfo.InputPropertiesInfo.Length && i < PayloadInputIndex; i++)
		{
			StatescriptNodeDiscovery.InputPropertyInfo info = typeInfo.InputPropertiesInfo[i];

			AddInputPropertyRow(
				new StatescriptNodeDiscovery.InputPropertyInfo(info.Label, info.ExpectedType, false),
				i,
				inputRoot);
		}

		FoldableContainer outputContainer = AddPropertySectionDivider(
			"Output Variables",
			OutputVariableColor,
			OutputFoldKey,
			GetFoldState(OutputFoldKey));

		var outputRoot = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		outputContainer.AddChild(outputRoot);

		for (int i = 0; i < typeInfo.OutputVariablesInfo.Length; i++)
		{
			StatescriptNodeDiscovery.OutputVariableInfo info = typeInfo.OutputVariablesInfo[i];
			AddBuiltInOutputRow(outputRoot, info.Label, i, info.ValueType);
		}

		BuildPayloadSection(outputRoot);
	}

	/// <inheritdoc/>
	internal override void Unbind()
	{
		base.Unbind();
		_payloadFoldable = null;
		_payloadProviderDropdown = null;
		_payloadOutputsContainer = null;
	}

	private static bool ScalarTypeMatches(StatescriptVariableType actual, StatescriptVariableType target)
	{
		if (actual == target)
		{
			return true;
		}

		// Float (32-bit) and Double (64-bit) both display as "Float" and are interchangeable for floating-point
		// outputs.
		bool actualIsFloating = actual is StatescriptVariableType.Float or StatescriptVariableType.Double;
		bool targetIsFloating = target is StatescriptVariableType.Float or StatescriptVariableType.Double;
		return actualIsFloating && targetIsFloating;
	}

	private static string ResolveObjectTypeId(Type valueType)
	{
		return StatescriptObjectVariableTypeRegistry.TryGetByClrType(
			valueType,
			out StatescriptObjectVariableType? descriptor)
				? descriptor.TypeId
				: string.Empty;
	}

	private void AddBuiltInOutputRow(VBoxContainer container, string label, int index, Type valueType)
	{
		List<string> candidates = GetCandidateVariableNames(valueType);

		string? current =
			FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver is VariableResolverResource resolver
				? resolver.VariableName
				: null;

		if (!string.IsNullOrEmpty(current) && !candidates.Contains(current))
		{
			current = null;
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}

		AddOutputVariableBadgeRow(
			container,
			label,
			$"_fold_output_{index}",
			candidates,
			current,
			variableName => OnBuiltInOutputSelected(variableName, index, valueType));
	}

	private void OnBuiltInOutputSelected(string? variableName, int index, Type valueType)
	{
		if (string.IsNullOrEmpty(variableName))
		{
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}
		else
		{
			bool isScalar = StatescriptVariableTypeConverter.TryFromSystemType(
				valueType,
				out StatescriptVariableType variableType);

			EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver = new VariableResolverResource
			{
				VariableName = variableName,
				Scope = VariableScope.Graph,
				ObjectTypeId = isScalar ? string.Empty : ResolveObjectTypeId(valueType),
				VariableType = isScalar ? variableType : StatescriptVariableType.Int,
				IsArray = false,
			};
		}

		RaisePropertyBindingChanged();
		ResetSize();
	}

	private List<string> GetCandidateVariableNames(Type valueType)
	{
		bool isScalar = StatescriptVariableTypeConverter.TryFromSystemType(
			valueType,
			out StatescriptVariableType targetType);
		string objectTypeId = isScalar ? string.Empty : ResolveObjectTypeId(valueType);

		var names = new List<string>();

		foreach (StatescriptGraphVariable variable in Graph.Variables)
		{
			if (variable.IsArray || string.IsNullOrEmpty(variable.VariableName))
			{
				continue;
			}

			bool matches = isScalar
				? string.IsNullOrEmpty(variable.ObjectTypeId) && ScalarTypeMatches(variable.VariableType, targetType)
				: !string.IsNullOrEmpty(variable.ObjectTypeId)
					&& (objectTypeId.Length == 0 || variable.ObjectTypeId == objectTypeId);

			if (matches)
			{
				names.Add(variable.VariableName);
			}
		}

		return names;
	}

	private void BuildPayloadSection(VBoxContainer container)
	{
		if (FindBinding(StatescriptPropertyDirection.Input, PayloadInputIndex)?.Resolver
			is EventPayloadOutputResolverResource resource)
		{
			_selectedPayloadProvider = EventPayloadProviderRegistry.ResolveIdentifier(resource.ProviderClassName);
			LoadPayloadBindings(resource);
		}

		_payloadFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			container,
			"Payload",
			GetFoldState(PayloadFoldKey, true));

		var providerRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_payloadFoldable.AddChild(providerRow);
		providerRow.AddChild(new Label
		{
			Text = "Provider:",
			CustomMinimumSize = new Vector2(75, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_payloadProviderDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		PopulatePayloadProviderDropdown();
		providerRow.AddChild(_payloadProviderDropdown);

		// The provider's declared outputs render nested inside the payload foldable so collapsing Payload hides them
		// behind the pill.
		_payloadOutputsContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_payloadFoldable.AddChild(_payloadOutputsContainer);
		RebuildPayloadOutputRows();

		UpdatePayloadBadge();

		_payloadFoldable.FoldingChanged += folded =>
		{
			SetFoldStateWithUndo(PayloadFoldKey, folded);
			UpdatePayloadBadge();
			RaisePropertyBindingChanged();
			ResetSize();
		};
		_payloadProviderDropdown.ItemSelected += OnPayloadProviderSelected;
	}

	private void UpdatePayloadBadge()
	{
		if (_payloadFoldable is null)
		{
			return;
		}

		// The provider selection reads as a resolver-style choice, so it uses the resolver badge (matching the raise
		// node's payload input) rather than the variable badge of the output rows.
		string summary = _payloadProviderDropdown is { Selected: > 0 } dropdown
			? dropdown.GetItemText(dropdown.Selected)
			: "(None)";

		InlineConstantSummaryFormatter.ApplyFoldableTitle(
			string.Empty,
			_payloadFoldable,
			summary,
			InlineSummaryBadgeKind.Resolver);
	}

	private void LoadPayloadBindings(EventPayloadOutputResolverResource resource)
	{
		_payloadVariableByOutput.Clear();

		for (int i = 0; i < resource.OutputNames.Count && i < resource.VariableNames.Count; i++)
		{
			_payloadVariableByOutput[resource.OutputNames[i]] = resource.VariableNames[i];
		}
	}

	private void PopulatePayloadProviderDropdown()
	{
		if (_payloadProviderDropdown is null)
		{
			return;
		}

		_payloadProviderDropdown.Clear();
		_payloadProviderClassNames.Clear();

		_payloadProviderDropdown.AddItem("(None)");
		_payloadProviderClassNames.Add(string.Empty);

		foreach (EventPayloadProviderRegistry.ProviderEntry entry in EventPayloadProviderRegistry.All)
		{
			_payloadProviderDropdown.AddItem(entry.DisplayName);
			_payloadProviderClassNames.Add(entry.Identifier);
		}

		if (!string.IsNullOrEmpty(_selectedPayloadProvider))
		{
			for (int i = 0; i < _payloadProviderClassNames.Count; i++)
			{
				if (_payloadProviderClassNames[i] == _selectedPayloadProvider)
				{
					_payloadProviderDropdown.Selected = i;
					return;
				}
			}
		}

		_payloadProviderDropdown.Selected = 0;
		_selectedPayloadProvider = string.Empty;
	}

	private void OnPayloadProviderSelected(long index)
	{
		if (_payloadProviderDropdown is null)
		{
			return;
		}

		int idx = _payloadProviderDropdown.Selected;
		_selectedPayloadProvider = idx >= 0 && idx < _payloadProviderClassNames.Count
			? _payloadProviderClassNames[idx]
			: string.Empty;

		RebuildPayloadOutputRows();
		UpdatePayloadBadge();
		SavePayloadResource();
		ResetSize();
	}

	private void RebuildPayloadOutputRows()
	{
		if (_payloadOutputsContainer is null)
		{
			return;
		}

		ClearContainer(_payloadOutputsContainer);

		if (string.IsNullOrEmpty(_selectedPayloadProvider)
			|| !EventPayloadProviderRegistry.TryGet(_selectedPayloadProvider, out IEventPayloadProvider provider))
		{
			return;
		}

		IReadOnlyList<EventPayloadOutput> declaredOutputs = provider.Outputs;

		for (int i = 0; i < declaredOutputs.Count; i++)
		{
			AddPayloadOutputRow(declaredOutputs[i].Name, declaredOutputs[i].ValueType);
		}
	}

	private void AddPayloadOutputRow(string outputName, Type valueType)
	{
		if (_payloadOutputsContainer is null)
		{
			return;
		}

		List<string> candidates = GetCandidateVariableNames(valueType);
		string stored = _payloadVariableByOutput.GetValueOrDefault(outputName, string.Empty);
		string? current = candidates.Contains(stored) ? stored : null;

		string capturedName = outputName;
		AddOutputVariableBadgeRow(
			_payloadOutputsContainer,
			outputName,
			$"_fold_payload_{outputName}",
			candidates,
			current,
			variableName =>
			{
				_payloadVariableByOutput[capturedName] = variableName ?? string.Empty;
				SavePayloadResource();
			});
	}

	private void SavePayloadResource()
	{
		if (string.IsNullOrEmpty(_selectedPayloadProvider))
		{
			RemoveBinding(StatescriptPropertyDirection.Input, PayloadInputIndex);
			RaisePropertyBindingChanged();
			return;
		}

		var resource = new EventPayloadOutputResolverResource { ProviderClassName = _selectedPayloadProvider };
		var outputNames = new Array<string>();
		var variableNames = new Array<string>();

		if (EventPayloadProviderRegistry.TryGet(_selectedPayloadProvider, out IEventPayloadProvider provider))
		{
			foreach (string outputName in provider.Outputs.Select(output => output.Name))
			{
				string variableName = _payloadVariableByOutput.GetValueOrDefault(outputName, string.Empty);

				if (string.IsNullOrEmpty(variableName))
				{
					continue;
				}

				outputNames.Add(outputName);
				variableNames.Add(variableName);
			}
		}

		resource.OutputNames = outputNames;
		resource.VariableNames = variableNames;
		EnsureBinding(StatescriptPropertyDirection.Input, PayloadInputIndex).Resolver = resource;
		RaisePropertyBindingChanged();
	}
}
#endif
