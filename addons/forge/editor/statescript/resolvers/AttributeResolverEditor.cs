// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Godot.Editor.Attributes;
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

	private AttributeSelectionControl? _attributePicker;
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

		_attributePicker = new AttributeSelectionControl
		{
			LabelWidth = LabelWidth,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_attributePicker.ValueChanged += OnAttributeSelectionChanged;
		root.AddChild(_attributePicker);

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

		root.AddChild(CreateEntitySelectorRow());

		_attributePicker.SetValue(_selectedSetClass, _selectedAttribute);
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

	private void OnAttributeSelectionChanged()
	{
		if (_attributePicker is null)
		{
			return;
		}

		_selectedSetClass = _attributePicker.SetClass;
		_selectedAttribute = _attributePicker.AttributeName;
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

	private void UpdateFinalChannelVisibility()
	{
		if (_finalChannelRow is not null)
		{
			_finalChannelRow.Visible = _calculationType == AttributeCalculationType.MagnitudeEvaluatedUpToChannel;
		}
	}
}
#endif
