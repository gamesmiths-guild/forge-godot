// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that binds a node input property to an activation data field. Uses a two-step selection: first
/// select the <see cref="IActivationDataProvider"/> implementation, then select a compatible field from that provider.
/// Providers are discovered via reflection.
/// </summary>
/// <remarks>
/// A graph supports only one activation data provider. Once any other node in the graph references a provider, the
/// provider dropdown is locked to that provider. The user only needs to clear the bindings on other nodes to unlock
/// the dropdown.
/// </remarks>
[Tool]
internal sealed partial class ActivationDataResolverEditor : NodeEditorProperty
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
	public override string DisplayName => "Activation Data";

	/// <inheritdoc/>
	public override string ResolverTypeId => "ActivationData";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return true;
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

		if (property?.Resolver is ActivationDataResolverResource activationRes)
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

		_providerDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
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

		_fieldDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateFieldDropdown();
		fieldRow.AddChild(_fieldDropdown);

		_providerDropdown.ItemSelected += OnProviderDropdownItemSelected;
		_fieldDropdown.ItemSelected += OnFieldDropdownItemSelected;
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ActivationDataResolverResource
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
		if (expectedType == typeof(Variant128))
		{
			return true;
		}

		Type actualType = StatescriptVariableTypeConverter.ToSystemType(fieldType);
		return expectedType == actualType;
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

				if (binding.Resolver is ActivationDataResolverResource { ProviderClassName.Length: > 0 } resolver)
				{
					return resolver.ProviderClassName;
				}
			}
		}

		return string.Empty;
	}

	private static IActivationDataProvider? InstantiateProvider(string className)
	{
		if (string.IsNullOrEmpty(className))
		{
			return null;
		}

		Type? type = ResolveProviderType(className);

		if (type is null)
		{
			return null;
		}

		return Activator.CreateInstance(type) as IActivationDataProvider;
	}

	private static IEnumerable<Type> GetProviderTypes()
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(x => typeof(IActivationDataProvider).IsAssignableFrom(x)
				&& !x.IsAbstract
				&& !x.IsInterface);
	}

	private static string GetProviderIdentifier(Type type)
	{
		return type.FullName ?? type.Name;
	}

	private static bool ProviderMatches(Type type, string providerIdentifier)
	{
		return type.AssemblyQualifiedName == providerIdentifier
			|| type.FullName == providerIdentifier
			|| type.Name == providerIdentifier;
	}

	private static Type? ResolveProviderType(string providerIdentifier)
	{
		return GetProviderTypes().FirstOrDefault(x => ProviderMatches(x, providerIdentifier));
	}

	private static bool ProviderIdentifiersMatch(string left, string right)
	{
		if (left == right)
		{
			return true;
		}

		Type? leftType = ResolveProviderType(left);
		return leftType is not null && ProviderMatches(leftType, right);
	}

	private void OnProviderDropdownItemSelected(long index)
	{
		if (_providerDropdown is null)
		{
			return;
		}

		var idx = _providerDropdown.Selected;
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

		var dropdownIndex = _fieldDropdown.Selected;

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
		var graphLockedProvider = _graph is not null
			? FindExistingProvider(_graph, _currentProperty)
			: string.Empty;

		if (!string.IsNullOrEmpty(graphLockedProvider))
		{
			// Another node already uses a provider: only show that one (plus None).
			Type? lockedProviderType = ResolveProviderType(graphLockedProvider);
			string lockedProviderIdentifier = lockedProviderType is null
				? graphLockedProvider
				: GetProviderIdentifier(lockedProviderType);
			string lockedProviderDisplayName = lockedProviderType?.Name ?? graphLockedProvider;

			_providerDropdown.AddItem(lockedProviderDisplayName);
			_providerClassNames.Add(lockedProviderIdentifier);
		}
		else
		{
			foreach (Type providerType in GetProviderTypes())
			{
				_providerDropdown.AddItem(providerType.Name);
				_providerClassNames.Add(GetProviderIdentifier(providerType));
			}
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedProviderClassName))
		{
			for (var i = 0; i < _providerClassNames.Count; i++)
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

		IActivationDataProvider? provider = InstantiateProvider(_selectedProviderClassName);

		if (provider is not null)
		{
			foreach (ForgeActivationDataField field in provider.GetFields())
			{
				if (string.IsNullOrEmpty(field.FieldName))
				{
					continue;
				}

				if (!IsCompatibleType(_expectedType, field.FieldType))
				{
					continue;
				}

				_fieldDropdown.AddItem(field.FieldName);
				_fieldNames.Add(field.FieldName);
			}
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedFieldName))
		{
			for (var i = 0; i < _fieldNames.Count; i++)
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

		IActivationDataProvider? provider = InstantiateProvider(_selectedProviderClassName);

		if (provider is null)
		{
			_selectedFieldType = StatescriptVariableType.Int;
			return;
		}

		foreach (ForgeActivationDataField field in provider.GetFields())
		{
			if (field.FieldName == _selectedFieldName)
			{
				_selectedFieldType = field.FieldType;
				return;
			}
		}

		_selectedFieldType = StatescriptVariableType.Int;
	}
}
#endif
