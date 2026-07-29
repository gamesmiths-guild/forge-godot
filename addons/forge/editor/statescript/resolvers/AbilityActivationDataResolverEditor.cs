// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that binds a node input property to an activation data field. Uses a two-step selection: first
/// select the <see cref="IAbilityActivationDataProvider"/> implementation, then select a compatible field from that
/// provider's declared outputs.
/// </summary>
/// <remarks>
/// <para>This is the read side of the same provider that builds the data on the sending graph, so one implementation
/// covers both directions. Providers are discovered through <see cref="AbilityActivationDataProviderRegistry"/>.</para>
/// <para>A graph supports only one activation data provider. Once any other node in the graph references a provider,
/// the provider dropdown is locked to that provider. The user only needs to clear the bindings on other nodes to unlock
/// the dropdown.</para>
/// </remarks>
[Tool]
internal sealed partial class AbilityActivationDataResolverEditor : NodeEditorProperty
{
	private readonly List<string> _providerClassNames = [];
	private readonly List<string> _fieldNames = [];

	private StatescriptGraph? _graph;
	private StatescriptNodeProperty? _currentProperty;

	private OptionButton? _providerDropdown;
	private OptionButton? _fieldDropdown;
	private Action? _onChanged;
	private Type _expectedType = typeof(Variant128);

	private string _selectedProviderClassName = string.Empty;
	private string _selectedFieldName = string.Empty;
	private StatescriptVariableType _selectedFieldType = StatescriptVariableType.Int;

	/// <inheritdoc/>
	public override string DisplayName => "Ability Activation Data";

	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityActivationData";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(object)
			|| expectedType == typeof(Variant128)
			|| StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;
		_expectedType = expectedType;
		_graph = graph;
		_currentProperty = property;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (property?.Resolver is AbilityActivationDataResolverResource activationRes)
		{
			_selectedProviderClassName = activationRes.ProviderClassName;
			_selectedFieldName = activationRes.FieldName;
			_selectedFieldType = activationRes.FieldType;
		}

		var providerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(providerRow);

		providerRow.AddChild(new Label
		{
			Text = "Provider:",
			CustomMinimumSize = new Vector2(75, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_providerDropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateProviderDropdown();
		providerRow.AddChild(_providerDropdown);

		// Re-scan the graph each time the dropdown opens to pick up changes from other editors.
		_providerDropdown.GetPopup().AboutToPopup += PopulateProviderDropdown;

		var fieldRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(fieldRow);

		fieldRow.AddChild(new Label
		{
			Text = "Field:",
			CustomMinimumSize = new Vector2(75, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_fieldDropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateFieldDropdown();
		fieldRow.AddChild(_fieldDropdown);

		_providerDropdown.ItemSelected += OnProviderDropdownItemSelected;
		_fieldDropdown.ItemSelected += OnFieldDropdownItemSelected;
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AbilityActivationDataResolverResource
		{
			ProviderClassName = _selectedProviderClassName,
			FieldName = _selectedFieldName,
			FieldType = _selectedFieldType,
		};
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
	}

	private static bool IsCompatibleType(Type expectedType, StatescriptVariableType fieldType)
	{
		return StatescriptVariableTypeConverter.IsCompatible(expectedType, fieldType);
	}

	private static string FindExistingProvider(StatescriptGraph graph, StatescriptNodeProperty? currentProperty)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				// Skip the property we're currently editing — the user should be free to change it.
				if (ReferenceEquals(binding, currentProperty))
				{
					continue;
				}

				if (binding.Resolver
					is AbilityActivationDataResolverResource { ProviderClassName.Length: > 0 } resolver)
				{
					return resolver.ProviderClassName;
				}
			}
		}

		return string.Empty;
	}

	private static string GetProviderDisplayName(string identifier)
	{
		foreach (AbilityActivationDataProviderRegistry.ProviderEntry entry
			in AbilityActivationDataProviderRegistry.All)
		{
			if (entry.Identifier == identifier)
			{
				return entry.DisplayName;
			}
		}

		return identifier;
	}

	private static IAbilityActivationDataProvider? InstantiateProvider(string className)
	{
		return AbilityActivationDataProviderRegistry.TryGet(
			className,
			out IAbilityActivationDataProvider provider)
			? provider
			: null;
	}

	private static bool ProviderIdentifiersMatch(string left, string right)
	{
		return left == right
			|| AbilityActivationDataProviderRegistry.ResolveIdentifier(left)
				== AbilityActivationDataProviderRegistry.ResolveIdentifier(right);
	}

	private void OnProviderDropdownItemSelected(long index)
	{
		if (_providerDropdown is null)
		{
			return;
		}

		int idx = _providerDropdown.Selected;
		_selectedProviderClassName = idx >= 0 && idx < _providerClassNames.Count
			? _providerClassNames[idx]
			: string.Empty;
		_selectedFieldName = string.Empty;
		_selectedFieldType = StatescriptVariableType.Int;

		PopulateFieldDropdown();

		_onChanged?.Invoke();
	}

	private void OnFieldDropdownItemSelected(long index)
	{
		if (_fieldDropdown is null)
		{
			return;
		}

		int dropdownIndex = _fieldDropdown.Selected;

		if (dropdownIndex >= 0 && dropdownIndex < _fieldNames.Count)
		{
			_selectedFieldName = _fieldNames[dropdownIndex];

			if (!string.IsNullOrEmpty(_selectedFieldName))
			{
				ResolveFieldType();
			}
			else
			{
				_selectedFieldType = StatescriptVariableType.Int;
			}
		}
		else
		{
			_selectedFieldName = string.Empty;
			_selectedFieldType = StatescriptVariableType.Int;
		}

		_onChanged?.Invoke();
	}

	private void PopulateProviderDropdown()
	{
		if (_providerDropdown is null)
		{
			return;
		}

		_providerDropdown.Clear();
		_providerClassNames.Clear();

		// Always add a (None) option to allow deselecting.
		_providerDropdown.AddItem("(None)");
		_providerClassNames.Add(string.Empty);

		// Re-scan the graph each time to pick up changes from other editors.
		string graphLockedProvider = _graph is not null
			? FindExistingProvider(_graph, _currentProperty)
			: string.Empty;

		if (!string.IsNullOrEmpty(graphLockedProvider))
		{
			// Another node already uses a provider: only show that one (plus None).
			string lockedProviderIdentifier =
				AbilityActivationDataProviderRegistry.ResolveIdentifier(graphLockedProvider);

			_providerDropdown.AddItem(GetProviderDisplayName(lockedProviderIdentifier));
			_providerClassNames.Add(lockedProviderIdentifier);
		}
		else
		{
			foreach (AbilityActivationDataProviderRegistry.ProviderEntry entry
				in AbilityActivationDataProviderRegistry.All)
			{
				_providerDropdown.AddItem(entry.DisplayName);
				_providerClassNames.Add(entry.Identifier);
			}
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedProviderClassName))
		{
			for (int i = 0; i < _providerClassNames.Count; i++)
			{
				if (ProviderIdentifiersMatch(_providerClassNames[i], _selectedProviderClassName))
				{
					_selectedProviderClassName = _providerClassNames[i];
					_providerDropdown.Selected = i;
					return;
				}
			}
		}

		// Default to (None).
		_providerDropdown.Selected = 0;
		_selectedProviderClassName = string.Empty;
	}

	private void PopulateFieldDropdown()
	{
		if (_fieldDropdown is null)
		{
			return;
		}

		_fieldDropdown.Clear();
		_fieldNames.Clear();

		// Always add a (None) option.
		_fieldDropdown.AddItem("(None)");
		_fieldNames.Add(string.Empty);

		IAbilityActivationDataProvider? provider = InstantiateProvider(_selectedProviderClassName);

		if (provider is not null)
		{
			foreach (AbilityActivationDataMember member in provider.Members)
			{
				// Members the graph has no variable type for (and so cannot bind) are simply not offered.
				if (string.IsNullOrEmpty(member.Name)
					|| !StatescriptVariableTypeConverter.TryFromSystemType(
						member.ValueType,
						out StatescriptVariableType fieldType)
					|| !IsCompatibleType(_expectedType, fieldType))
				{
					continue;
				}

				_fieldDropdown.AddItem(member.Name);
				_fieldNames.Add(member.Name);
			}
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedFieldName))
		{
			for (int i = 0; i < _fieldNames.Count; i++)
			{
				if (_fieldNames[i] == _selectedFieldName)
				{
					_fieldDropdown.Selected = i;
					return;
				}
			}
		}

		// Default to (None).
		_fieldDropdown.Selected = 0;
		_selectedFieldName = string.Empty;
	}

	private void ResolveFieldType()
	{
		if (string.IsNullOrEmpty(_selectedProviderClassName) || string.IsNullOrEmpty(_selectedFieldName))
		{
			_selectedFieldType = StatescriptVariableType.Int;
			return;
		}

		IAbilityActivationDataProvider? provider = InstantiateProvider(_selectedProviderClassName);

		if (provider is null)
		{
			_selectedFieldType = StatescriptVariableType.Int;
			return;
		}

		foreach (AbilityActivationDataMember member in provider.Members)
		{
			if (member.Name == _selectedFieldName
				&& StatescriptVariableTypeConverter.TryFromSystemType(
					member.ValueType,
					out StatescriptVariableType fieldType))
			{
				_selectedFieldType = fieldType;
				return;
			}
		}

		_selectedFieldType = StatescriptVariableType.Int;
	}
}
#endif
