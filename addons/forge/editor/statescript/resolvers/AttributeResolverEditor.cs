// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AttributeResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private enum EntitySelection
	{
		Owner = 0,
		Source = 1,
		Target = 2,
		Variable = 3,
	}

	private OptionButton? _setDropdown;
	private OptionButton? _attributeDropdown;
	private OptionButton? _calculationDropdown;
	private OptionButton? _entityDropdown;
	private SpinBox? _finalChannelSpin;
	private Control? _finalChannelRow;
	private Control? _entityEditorRow;
	private VBoxContainer? _entityEditorContainer;
	private EntityVariableResolverEditor? _entityVariableEditor;

	private string _selectedSetClass = string.Empty;
	private string _selectedAttribute = string.Empty;
	private AttributeCalculationType _calculationType;
	private int _finalChannel;
	private EntitySelection _entitySelection = EntitySelection.Owner;
	private Action? _onChanged;
	private StatescriptGraph? _graph;

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
		_graph = graph;
		_onChanged = onChanged;

		if (property?.Resolver is AttributeResolverResource resource)
		{
			_selectedSetClass = resource.AttributeSetClass;
			_selectedAttribute = resource.AttributeName;
			_calculationType = resource.CalculationType;
			_finalChannel = resource.FinalChannel;
			_entitySelection = ResolveEntitySelection(resource.EntityResolver);
		}

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_setDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_setDropdown.ItemSelected += OnSetChanged;
		root.AddChild(CreateLabeledRow("Set:", _setDropdown));

		_attributeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_attributeDropdown.ItemSelected += OnAttributeChanged;
		root.AddChild(CreateLabeledRow("Attr:", _attributeDropdown));

		_calculationDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (AttributeCalculationType value in Enum.GetValues<AttributeCalculationType>())
		{
			_calculationDropdown.AddItem(value.ToString());
		}

		_calculationDropdown.Selected = (int)_calculationType;
		_calculationDropdown.ItemSelected += OnCalculationChanged;
		root.AddChild(CreateLabeledRow("Calc:", _calculationDropdown));

		_finalChannelSpin = new SpinBox
		{
			MinValue = 0,
			Step = 1,
			Value = _finalChannel,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_finalChannelSpin.ValueChanged += OnFinalChannelChanged;
		_finalChannelRow = CreateLabeledRow("Chan:", _finalChannelSpin);
		root.AddChild(_finalChannelRow);

		_entityDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_entityDropdown.AddItem("Owner");
		_entityDropdown.AddItem("Source");
		_entityDropdown.AddItem("Target");
		_entityDropdown.AddItem("Variable");
		_entityDropdown.Selected = (int)_entitySelection;
		_entityDropdown.ItemSelected += OnEntityChanged;
		root.AddChild(CreateLabeledRow("Entity:", _entityDropdown));

		_entityEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_entityEditorRow = CreateIndentedRow(_entityEditorContainer);
		root.AddChild(_entityEditorRow);

		PopulateSetDropdown();
		PopulateAttributeDropdown();
		RebuildEntityEditor(property?.Resolver as AttributeResolverResource);
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

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_entityVariableEditor?.ClearCallbacks();
	}

	private static EntitySelection ResolveEntitySelection(EntityResolverResourceBase? resource)
	{
		return resource switch
		{
			SourceEntityResolverResource => EntitySelection.Source,
			TargetEntityResolverResource => EntitySelection.Target,
			EntityVariableResolverResource => EntitySelection.Variable,
			_ => EntitySelection.Owner,
		};
	}

	private static HBoxContainer CreateLabeledRow(string labelText, Control editor)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(LabelWidth, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});
		row.AddChild(editor);
		return row;
	}

	private static HBoxContainer CreateIndentedRow(Control editor)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			CustomMinimumSize = new Vector2(LabelWidth, 0),
		});
		row.AddChild(editor);
		return row;
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
		_onChanged?.Invoke();
	}

	private void OnAttributeChanged(long index)
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_selectedAttribute = _attributeDropdown.GetItemText(_attributeDropdown.Selected);
		_onChanged?.Invoke();
	}

	private void OnCalculationChanged(long index)
	{
		_calculationType = (AttributeCalculationType)(int)index;
		UpdateFinalChannelVisibility();
		_onChanged?.Invoke();
	}

	private void OnFinalChannelChanged(double value)
	{
		_finalChannel = (int)value;
		_onChanged?.Invoke();
	}

	private void OnEntityChanged(long index)
	{
		_entitySelection = (EntitySelection)(int)index;
		RebuildEntityEditor(null);
		_onChanged?.Invoke();
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

	private void RebuildEntityEditor(AttributeResolverResource? existingResource)
	{
		if (_entityEditorContainer is null)
		{
			return;
		}

		foreach (global::Godot.Node child in _entityEditorContainer.GetChildren())
		{
			_entityEditorContainer.RemoveChild(child);
			child.Free();
		}

		_entityVariableEditor = null;

		if (_entitySelection != EntitySelection.Variable || _graph is null)
		{
			if (_entityEditorRow is not null)
			{
				_entityEditorRow.Visible = false;
			}

			RaiseLayoutSizeChanged();
			return;
		}

		StatescriptNodeProperty? entityProperty = existingResource?.EntityResolver
			is EntityVariableResolverResource entityVariable
				? new StatescriptNodeProperty { Resolver = entityVariable }
				: null;

		_entityVariableEditor = new EntityVariableResolverEditor();
		_entityVariableEditor.Setup(
			_graph,
			entityProperty,
			typeof(IForgeEntity),
			() =>
			{
				_onChanged?.Invoke();
				RaiseLayoutSizeChanged();
			},
			false);
		_entityVariableEditor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		_entityEditorContainer.AddChild(_entityVariableEditor);

		if (_entityEditorRow is not null)
		{
			_entityEditorRow.Visible = true;
		}

		RaiseLayoutSizeChanged();
	}

	private void UpdateFinalChannelVisibility()
	{
		if (_finalChannelRow is not null)
		{
			_finalChannelRow.Visible = _calculationType == AttributeCalculationType.MagnitudeEvaluatedUpToChannel;
		}
	}

	private EntityResolverResourceBase? BuildEntityResolverResource()
	{
		return _entitySelection switch
		{
			EntitySelection.Source => new SourceEntityResolverResource(),
			EntitySelection.Target => new TargetEntityResolverResource(),
			EntitySelection.Variable => BuildEntityVariableResolver(),
			_ => new OwnerEntityResolverResource(),
		};
	}

	private EntityResolverResourceBase BuildEntityVariableResolver()
	{
		if (_entityVariableEditor is null)
		{
			return new EntityVariableResolverResource();
		}

		var tempProperty = new StatescriptNodeProperty();
		_entityVariableEditor.SaveTo(tempProperty);
		return (EntityVariableResolverResource?)tempProperty.Resolver ?? new EntityVariableResolverResource();
	}
}
#endif
