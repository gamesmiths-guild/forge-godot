// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that binds the raise-event node's optional payload input to an <c>IEventPayloadProvider</c>. The
/// provider dropdown lists every provider discovered in the project assembly, plus a <c>(None)</c> option that leaves
/// the input unbound. When the selected provider declares inputs, each one is rendered as a nested resolver section so
/// designers can author the value the provider receives.
/// </summary>
[Tool]
internal sealed partial class EventPayloadResolverEditor : NodeEditorProperty
{
	private readonly List<string> _providerClassNames = [];
	private readonly List<InputSection> _inputSections = [];
	private readonly System.Collections.Generic.Dictionary<string, StatescriptResolverResource?> _storedResolvers = [];

	private StatescriptGraph? _graph;
	private OptionButton? _providerDropdown;
	private VBoxContainer? _inputsContainer;
	private Action? _onChanged;

	private string _selectedProviderClassName = string.Empty;

	/// <inheritdoc/>
	public override string DisplayName => "Event Payload";

	/// <inheritdoc/>
	public override string ResolverTypeId => "EventPayload";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(EventPayloadRaiser);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_graph = graph;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		if (property?.Resolver is EventPayloadResolverResource payloadRes)
		{
			_selectedProviderClassName =
				EventPayloadProviderRegistry.ResolveIdentifier(payloadRes.ProviderClassName);
			LoadStoredResolvers(payloadRes);
		}

		var providerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(providerRow);

		providerRow.AddChild(new Label
		{
			Text = "Provider:",
			CustomMinimumSize = new Vector2(75, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_providerDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateProviderDropdown();
		providerRow.AddChild(_providerDropdown);

		_inputsContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_inputsContainer);
		RebuildInputSections();

		_providerDropdown.ItemSelected += OnProviderDropdownItemSelected;
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		CaptureCurrentResolvers();

		var resource = new EventPayloadResolverResource
		{
			ProviderClassName = _selectedProviderClassName,
		};

		var names = new Array<string>();
		var resolvers = new Array<StatescriptResolverResource>();

		for (int i = 0; i < _inputSections.Count; i++)
		{
			InputSection section = _inputSections[i];

			if (_storedResolvers.TryGetValue(section.Name, out StatescriptResolverResource? resolver)
				&& resolver is not null)
			{
				names.Add(section.Name);
				resolvers.Add(resolver);
			}
		}

		resource.InputNames = names;
		resource.InputResolvers = resolvers;
		property.Resolver = resource;
	}

	/// <inheritdoc/>
	public override bool TryGetInlineSummary(out string summary)
	{
		summary = string.IsNullOrEmpty(_selectedProviderClassName)
			? "(None)"
			: GetProviderDisplayName(_selectedProviderClassName);
		return true;
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;

		for (int i = 0; i < _inputSections.Count; i++)
		{
			_inputSections[i].Editor?.ClearCallbacks();
		}

		_inputSections.Clear();
		_providerDropdown = null;
		_inputsContainer = null;
		_graph = null;
	}

	private static string GetProviderDisplayName(string identifier)
	{
		foreach (EventPayloadProviderRegistry.ProviderEntry entry in EventPayloadProviderRegistry.All)
		{
			if (entry.Identifier == identifier)
			{
				return entry.DisplayName;
			}
		}

		return identifier;
	}

	private void LoadStoredResolvers(EventPayloadResolverResource resource)
	{
		_storedResolvers.Clear();

		for (int i = 0; i < resource.InputNames.Count && i < resource.InputResolvers.Count; i++)
		{
			_storedResolvers[resource.InputNames[i]] = resource.InputResolvers[i];
		}
	}

	private void PopulateProviderDropdown()
	{
		if (_providerDropdown is null)
		{
			return;
		}

		_providerDropdown.Clear();
		_providerClassNames.Clear();

		// Always add a (None) option so the optional input can be left unbound.
		_providerDropdown.AddItem("(None)");
		_providerClassNames.Add(string.Empty);

		foreach (EventPayloadProviderRegistry.ProviderEntry entry in EventPayloadProviderRegistry.All)
		{
			_providerDropdown.AddItem(entry.DisplayName);
			_providerClassNames.Add(entry.Identifier);
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedProviderClassName))
		{
			for (int i = 0; i < _providerClassNames.Count; i++)
			{
				if (_providerClassNames[i] == _selectedProviderClassName)
				{
					_providerDropdown.Selected = i;
					return;
				}
			}
		}

		// Default to (None).
		_providerDropdown.Selected = 0;
		_selectedProviderClassName = string.Empty;
	}

	private void OnProviderDropdownItemSelected(long index)
	{
		if (_providerDropdown is null)
		{
			return;
		}

		// Preserve authored resolvers so switching back to a provider restores its inputs.
		CaptureCurrentResolvers();

		int idx = _providerDropdown.Selected;
		_selectedProviderClassName = idx >= 0 && idx < _providerClassNames.Count
			? _providerClassNames[idx]
			: string.Empty;

		RebuildInputSections();
		NotifyChanged();
		RaiseLayoutSizeChanged();
	}

	private void RebuildInputSections()
	{
		if (_inputsContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_inputsContainer);
		_inputSections.Clear();

		if (string.IsNullOrEmpty(_selectedProviderClassName)
			|| !EventPayloadProviderRegistry.TryGet(_selectedProviderClassName, out IEventPayloadProvider provider))
		{
			return;
		}

		IReadOnlyList<EventPayloadInput> declaredInputs = provider.Inputs;

		for (int i = 0; i < declaredInputs.Count; i++)
		{
			BuildInputSection(declaredInputs[i]);
		}
	}

	private void BuildInputSection(EventPayloadInput input)
	{
		if (_inputsContainer is null)
		{
			return;
		}

		List<Func<NodeEditorProperty>> factories = StatescriptResolverRegistry.GetCompatibleFactories(input.ValueType);
		factories.RemoveAll(factory => !StatescriptResolverRegistry.SupportsScalarValues(factory));

		FoldableContainer foldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			_inputsContainer,
			$"{input.Name}:",
			false);

		foldable.FoldingChanged += OnFoldableChanged;

		var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(content);

		if (factories.Count == 0)
		{
			var errorLabel = new Label { Text = "No compatible resolvers." };
			errorLabel.AddThemeColorOverride("font_color", Colors.Red);
			content.AddChild(errorLabel);
			return;
		}

		StatescriptResolverResource? existing = _storedResolvers.GetValueOrDefault(input.Name);
		OptionButton dropdown = NestedResolverEditorUtilities.CreateResolverDropdownControl(factories, existing);
		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		content.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(dropdown));
		content.AddChild(editorContainer);

		var section = new InputSection(input.Name, input.ValueType, factories, editorContainer)
		{
			Foldable = foldable,
		};

		_inputSections.Add(section);

		ShowInputEditor(section, dropdown.Selected, existing);
		UpdateAllInputSectionTitles();

		dropdown.ItemSelected += selected => OnInputResolverDropdownItemSelected(section, (int)selected);
	}

	private void ShowInputEditor(InputSection section, int factoryIndex, StatescriptResolverResource? existing)
	{
		NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
			_graph,
			section.Factories,
			factoryIndex,
			existing,
			[section.ValueType],
			OnNestedChanged,
			RaiseLayoutSizeChanged);

		if (editor is null)
		{
			return;
		}

		section.EditorContainer.AddChild(editor);
		section.Editor = editor;
	}

	private void OnInputResolverDropdownItemSelected(InputSection section, int index)
	{
		NestedResolverEditorUtilities.ClearContainer(section.EditorContainer);
		section.Editor = null;
		ShowInputEditor(section, index, null);
		UpdateAllInputSectionTitles();
		NotifyChanged();
		RaiseLayoutSizeChanged();
	}

	private void OnFoldableChanged(bool folded)
	{
		UpdateAllInputSectionTitles();
		RaiseLayoutSizeChanged();
	}

	private void OnNestedChanged()
	{
		UpdateAllInputSectionTitles();
		NotifyChanged();
	}

	private void UpdateAllInputSectionTitles()
	{
		for (int i = 0; i < _inputSections.Count; i++)
		{
			InputSection section = _inputSections[i];

			if (section.Foldable is not null)
			{
				InlineConstantSummaryFormatter.ApplyFoldableTitle($"{section.Name}:", section.Foldable, section.Editor);
			}
		}
	}

	private void CaptureCurrentResolvers()
	{
		for (int i = 0; i < _inputSections.Count; i++)
		{
			InputSection section = _inputSections[i];

			if (section.Editor is null)
			{
				continue;
			}

			var tempProperty = new StatescriptNodeProperty();
			section.Editor.SaveTo(tempProperty);
			_storedResolvers[section.Name] = tempProperty.Resolver;
		}
	}

	private void NotifyChanged()
	{
		_onChanged?.Invoke();
	}

	private sealed class InputSection(
		string name,
		Type valueType,
		List<Func<NodeEditorProperty>> factories,
		VBoxContainer editorContainer)
	{
		public string Name { get; } = name;

		public Type ValueType { get; } = valueType;

		public List<Func<NodeEditorProperty>> Factories { get; } = factories;

		public VBoxContainer EditorContainer { get; } = editorContainer;

		public NodeEditorProperty? Editor { get; set; }

		public FoldableContainer? Foldable { get; set; }
	}
}
#endif
