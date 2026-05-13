// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AttributeResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 60.0f;

	private OptionButton? _setDropdown;
	private OptionButton? _attributeDropdown;
	private OptionButton? _calculationDropdown;
	private SpinBox? _finalChannelSpin;
	private Control? _finalChannelRow;

	private string _selectedSetClass = string.Empty;
	private string _selectedAttribute = string.Empty;
	private AttributeCalculationType _calculationType;
	private int _finalChannel;

	public override string DisplayName => "Attribute";

	public override string ResolverTypeId => "Attribute";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(int) || expectedType == typeof(Variant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var existingResource = property?.Resolver as AttributeResolverResource;

		if (existingResource is not null)
		{
			_selectedSetClass = existingResource.AttributeSetClass;
			_selectedAttribute = existingResource.AttributeName;
			_calculationType = existingResource.CalculationType;
			_finalChannel = existingResource.FinalChannel;
		}

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_setDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_setDropdown.ItemSelected += OnSetChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Set:", _setDropdown, LabelWidth));

		_attributeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_attributeDropdown.ItemSelected += OnAttributeChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Attr:", _attributeDropdown, LabelWidth));

		_calculationDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (AttributeCalculationType value in Enum.GetValues<AttributeCalculationType>())
		{
			_calculationDropdown.AddItem(value.ToString());
		}

		_calculationDropdown.Selected = (int)_calculationType;
		_calculationDropdown.ItemSelected += OnCalculationChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Calc:", _calculationDropdown, LabelWidth));

		_finalChannelSpin = new SpinBox
		{
			MinValue = 0,
			Step = 1,
			Value = _finalChannel,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_finalChannelSpin.ValueChanged += OnFinalChannelChanged;
		_finalChannelRow = ResolverEditorLayoutUtilities.CreateLabeledRow("Chan:", _finalChannelSpin, LabelWidth);
		root.AddChild(_finalChannelRow);

		root.AddChild(CreateEntitySelectorRow(LabelWidth));
		root.AddChild(CreateEntityScopeEditorRow(LabelWidth));

		PopulateSetDropdown();
		PopulateAttributeDropdown();
		PopulateEntityScopeEditor(existingResource?.EntityResolver);
		UpdateFinalChannelVisibility();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AttributeResolverResource
		{
			AttributeSetClass = _selectedSetClass,
			AttributeName = _selectedAttribute,
			CalculationType = _calculationType,
			FinalChannel = _finalChannel,
			EntityResolver = BuildEntityResolverResource(),
		};
	}

	private void OnSetChanged(long index)
	{
		if (_setDropdown is null)
		{
			return;
		}

		_selectedSetClass = _setDropdown.GetItemText(_setDropdown.Selected);
		_selectedAttribute = string.Empty;
		PopulateAttributeDropdown();
		NotifyChanged();
	}

	private void OnAttributeChanged(long index)
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_selectedAttribute = _attributeDropdown.GetItemText(_attributeDropdown.Selected);
		NotifyChanged();
	}

	private void OnCalculationChanged(long index)
	{
		_calculationType = (AttributeCalculationType)(int)index;
		UpdateFinalChannelVisibility();
		NotifyChanged();
	}

	private void OnFinalChannelChanged(double value)
	{
		_finalChannel = (int)value;
		NotifyChanged();
	}

	private void PopulateSetDropdown()
	{
		if (_setDropdown is null)
		{
			return;
		}

		_setDropdown.Clear();
		foreach (string option in EditorUtils.GetAttributeSetOptions())
		{
			_setDropdown.AddItem(option);
		}

		for (int i = 0; i < _setDropdown.ItemCount; i++)
		{
			if (_setDropdown.GetItemText(i) == _selectedSetClass)
			{
				_setDropdown.Selected = i;
				return;
			}
		}

		if (_setDropdown.ItemCount > 0)
		{
			_setDropdown.Selected = 0;
			_selectedSetClass = _setDropdown.GetItemText(0);
		}
	}

	private void PopulateAttributeDropdown()
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_attributeDropdown.Clear();
		foreach (string option in EditorUtils.GetAttributeOptions(_selectedSetClass))
		{
			_attributeDropdown.AddItem(option);
		}

		for (int i = 0; i < _attributeDropdown.ItemCount; i++)
		{
			if (_attributeDropdown.GetItemText(i) == _selectedAttribute)
			{
				_attributeDropdown.Selected = i;
				return;
			}
		}

		if (_attributeDropdown.ItemCount > 0)
		{
			_attributeDropdown.Selected = 0;
			_selectedAttribute = _attributeDropdown.GetItemText(0);
		}
	}

	private void UpdateFinalChannelVisibility()
	{
		if (_finalChannelRow is not null)
		{
			_finalChannelRow.Visible = _calculationType == AttributeCalculationType.MagnitudeEvaluatedUpToChannel;
		}
	}
}
#endif
