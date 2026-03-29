// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
/// A graph supports only one activation data provider. Once any node in the graph references a provider, the provider
/// dropdown is locked to that provider for all subsequent nodes. The user must remove all existing activation data
/// bindings before switching to a different provider.
/// </remarks>
[Tool]
internal sealed partial class ActivationDataResolverEditor : NodeEditorProperty
{
	private readonly List<string> _providerClassNames = [];
	private readonly List<string> _fieldNames = [];

	private OptionButton? _providerDropdown;
	private OptionButton? _fieldDropdown;
	private Action? _onChanged;
	private Type _expectedType = typeof(Variant128);

	private string _selectedProviderClassName = string.Empty;
	private string _selectedFieldName = string.Empty;
	private StatescriptVariableType _selectedFieldType = StatescriptVariableType.Int;

	private string _graphLockedProvider = string.Empty;

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

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (property?.Resolver is ActivationDataResolverResource activationRes)
		{
			_selectedProviderClassName = activationRes.ProviderClassName;
			_selectedFieldName = activationRes.FieldName;
			_selectedFieldType = activationRes.FieldType;
		}

		_graphLockedProvider = FindExistingProvider(graph, property);

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

	private static string FindExistingProvider(StatescriptGraph graph, StatescriptNodeProperty? currentProperty)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				if (binding == currentProperty)
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

		Type? type = Array.Find(
			Assembly.GetExecutingAssembly().GetTypes(),
			x => typeof(IActivationDataProvider).IsAssignableFrom(x)
				&& !x.IsAbstract
				&& !x.IsInterface
				&& x.Name == className);

		if (type is null)
		{
			return null;
		}

		return Activator.CreateInstance(type) as IActivationDataProvider;
	}

	private void OnProviderDropdownItemSelected(long index)
	{
		if (_providerDropdown is null)
		{
			return;
		}

		_selectedProviderClassName = _providerDropdown.GetItemText(_providerDropdown.Selected);
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

		var idx = _fieldDropdown.Selected;

		if (idx >= 0 && idx < _fieldNames.Count)
		{
			_selectedFieldName = _fieldNames[idx];
			ResolveFieldType();
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

		if (!string.IsNullOrEmpty(_graphLockedProvider))
		{
			// Another node already uses a provider, lock to that one.
			_providerDropdown.AddItem(_graphLockedProvider);
			_providerClassNames.Add(_graphLockedProvider);
			_providerDropdown.Selected = 0;
			_selectedProviderClassName = _graphLockedProvider;
			return;
		}

		Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		foreach (var name in allTypes
			.Where(x => typeof(IActivationDataProvider).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface)
			.Select(x => x.Name))
		{
			_providerDropdown.AddItem(name);
			_providerClassNames.Add(name);
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedProviderClassName))
		{
			for (var i = 0; i < _providerDropdown.GetItemCount(); i++)
			{
				if (_providerDropdown.GetItemText(i) == _selectedProviderClassName)
				{
					_providerDropdown.Selected = i;
					return;
				}
			}
		}

		// Default to first if available.
		if (_providerDropdown.GetItemCount() > 0)
		{
			_providerDropdown.Selected = 0;
			_selectedProviderClassName = _providerDropdown.GetItemText(0);
		}
	}

	private void PopulateFieldDropdown()
	{
		if (_fieldDropdown is null)
		{
			return;
		}

		_fieldDropdown.Clear();
		_fieldNames.Clear();

		IActivationDataProvider? provider = InstantiateProvider(_selectedProviderClassName);

		if (provider is not null)
		{
			foreach (ForgeActivationDataField field in provider.GetFields())
			{
				if (string.IsNullOrEmpty(field.FieldName))
				{
					continue;
				}

				if (_expectedType != typeof(Variant128)
					&& !StatescriptVariableTypeConverter.IsCompatible(_expectedType, field.FieldType))
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

		// Default to first if available.
		if (_fieldDropdown.GetItemCount() > 0)
		{
			_fieldDropdown.Selected = 0;
			_selectedFieldName = _fieldDropdown.GetItemText(0);
		}
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
