// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using Godot.Collections;
using GodotVariant = Godot.Variant;
using GodotVector2 = Godot.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that holds a constant (inline) value. The user edits the value directly in the node.
/// </summary>
[Tool]
internal sealed partial class VariantResolverEditor : NodeEditorProperty
{
	private StatescriptVariableType _valueType;
	private bool _isArray;
	private bool _isArrayExpanded;
	private GodotVariant _currentValue;
	private Array<GodotVariant> _arrayValues = [];
	private Action? _onChanged;

	private Button? _toggleButton;
	private VBoxContainer? _elementsContainer;

	/// <inheritdoc/>
	public override string DisplayName => "Constant";

	/// <inheritdoc/>
	public override string ResolverTypeId => "Variant";

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
		_isArray = isArray;
		_onChanged = onChanged;

		if (!StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _valueType))
		{
			_valueType = StatescriptVariableType.Int;
		}

		if (property?.Resolver is VariantResolverResource variantRes)
		{
			_valueType = variantRes.ValueType;

			if (_isArray)
			{
				_arrayValues = [.. variantRes.ArrayValues];
				_isArrayExpanded = variantRes.IsArrayExpanded;
			}
			else
			{
				_currentValue = variantRes.Value;
			}
		}
		else if (_isArray)
		{
			_arrayValues = [];
		}
		else
		{
			_currentValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(_valueType);
		}

		CustomMinimumSize = new GodotVector2(200, 40);

		if (_isArray)
		{
			VBoxContainer arrayEditor = CreateArrayEditor();
			AddChild(arrayEditor);
		}
		else
		{
			Control valueEditor = CreateValueEditor();
			AddChild(valueEditor);
		}
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		if (_isArray)
		{
			property.Resolver = new VariantResolverResource
			{
				ValueType = _valueType,
				IsArray = true,
				ArrayValues = [.. _arrayValues],
				IsArrayExpanded = _isArrayExpanded,
			};
		}
		else
		{
			property.Resolver = new VariantResolverResource
			{
				Value = _currentValue,
				ValueType = _valueType,
			};
		}
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
	}

	private Control CreateValueEditor()
	{
		if (_valueType == StatescriptVariableType.Bool)
		{
			return StatescriptEditorControls.CreateBoolEditor(_currentValue.AsBool(), OnBoolValueChanged);
		}

		if (StatescriptEditorControls.IsIntegerType(_valueType)
			|| StatescriptEditorControls.IsFloatType(_valueType))
		{
			return StatescriptEditorControls.CreateNumericSpinSlider(
				_valueType,
				_currentValue.AsDouble(),
				OnNumericValueChanged);
		}

		if (StatescriptEditorControls.IsVectorType(_valueType))
		{
			return StatescriptEditorControls.CreateVectorEditor(
				_valueType,
				x => StatescriptEditorControls.GetVectorComponent(_currentValue, _valueType, x),
				OnVectorValueChanged);
		}

		return new Label { Text = _valueType.ToString() };
	}

	private void OnBoolValueChanged(bool x)
	{
		_currentValue = GodotVariant.From(x);
		_onChanged?.Invoke();
	}

	private void OnNumericValueChanged(double x)
	{
		_currentValue = GodotVariant.From(x);
		_onChanged?.Invoke();
	}

	private void OnVectorValueChanged(double[] x)
	{
		_currentValue = StatescriptEditorControls.BuildVectorVariant(_valueType, x);
		_onChanged?.Invoke();
	}

	private VBoxContainer CreateArrayEditor()
	{
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		_elementsContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = _isArrayExpanded,
		};

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(headerRow);

		_toggleButton = new Button
		{
			Text = $"Array (size {_arrayValues.Count})",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ToggleMode = true,
			ButtonPressed = _isArrayExpanded,
		};

		_toggleButton.Toggled += OnArrayToggled;

		headerRow.AddChild(_toggleButton);

		Texture2D addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");

		var addButton = new Button
		{
			Icon = addIcon,
			Flat = true,
			TooltipText = "Add Element",
			CustomMinimumSize = new GodotVector2(24, 24),
		};

		addButton.Pressed += OnAddElementPressed;

		headerRow.AddChild(addButton);

		vBox.AddChild(_elementsContainer);

		RebuildArrayElements();

		return vBox;
	}

	private void OnArrayToggled(bool toggled)
	{
		if (_elementsContainer is not null)
		{
			_elementsContainer.Visible = toggled;
		}

		_isArrayExpanded = toggled;
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnAddElementPressed()
	{
		GodotVariant defaultValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(_valueType);
		_arrayValues.Add(defaultValue);
		_onChanged?.Invoke();

		if (_elementsContainer is not null)
		{
			_elementsContainer.Visible = true;
		}

		_isArrayExpanded = true;
		RebuildArrayElements();
		RaiseLayoutSizeChanged();
	}

	private void RebuildArrayElements()
	{
		if (_elementsContainer is null || _toggleButton is null)
		{
			return;
		}

		foreach (Node child in _elementsContainer.GetChildren())
		{
			_elementsContainer.RemoveChild(child);
			child.Free();
		}

		_toggleButton.Text = $"Array (size {_arrayValues.Count})";

		Texture2D removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		for (var i = 0; i < _arrayValues.Count; i++)
		{
			var capturedIndex = i;

			if (StatescriptEditorControls.IsVectorType(_valueType))
			{
				var elementVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				_elementsContainer.AddChild(elementVBox);

				var labelRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementVBox.AddChild(labelRow);
				labelRow.AddChild(new Label
				{
					Text = $"[{i}]",
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				});

				AddArrayRemoveButton(labelRow, removeIcon, capturedIndex);

				VBoxContainer vectorEditor = StatescriptEditorControls.CreateVectorEditor(
					_valueType,
					x =>
					{
						return StatescriptEditorControls.GetVectorComponent(
							_arrayValues[capturedIndex],
							_valueType,
							x);
					},
					x =>
					{
						_arrayValues[capturedIndex] =
							StatescriptEditorControls.BuildVectorVariant(_valueType, x);
						_onChanged?.Invoke();
					});

				elementVBox.AddChild(vectorEditor);
			}
			else
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				_elementsContainer.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				if (_valueType == StatescriptVariableType.Bool)
				{
					elementRow.AddChild(StatescriptEditorControls.CreateBoolEditor(
						_arrayValues[capturedIndex].AsBool(),
						x =>
						{
							_arrayValues[capturedIndex] = GodotVariant.From(x);
							_onChanged?.Invoke();
						}));
				}
				else
				{
					EditorSpinSlider spin = StatescriptEditorControls.CreateNumericSpinSlider(
						_valueType,
						_arrayValues[capturedIndex].AsDouble(),
						x =>
						{
							_arrayValues[capturedIndex] = GodotVariant.From(x);
							_onChanged?.Invoke();
						});

					elementRow.AddChild(spin);
				}

				AddArrayRemoveButton(elementRow, removeIcon, capturedIndex);
			}
		}
	}

	private void AddArrayRemoveButton(
		HBoxContainer row,
		Texture2D removeIcon,
		int elementIndex)
	{
		var removeButton = new Button
		{
			Icon = removeIcon,
			Flat = true,
			TooltipText = "Remove Element",
			CustomMinimumSize = new GodotVector2(24, 24),
		};

		var handler = new ArrayRemoveHandler(this, elementIndex);
		removeButton.AddChild(handler);
		removeButton.Pressed += handler.HandlePressed;

		row.AddChild(removeButton);
	}

	private void OnRemoveElement(int elementIndex)
	{
		_arrayValues.RemoveAt(elementIndex);
		_onChanged?.Invoke();
		RebuildArrayElements();
		RaiseLayoutSizeChanged();
	}

	/// <summary>
	/// Godot-compatible signal handler for array element remove buttons. Holds the element index and a reference to the
	/// owning editor so the <c>Pressed</c> signal can be handled without a lambda.
	/// </summary>
	[Tool]
	private sealed partial class ArrayRemoveHandler : Node
	{
		private readonly VariantResolverEditor _editor;
		private readonly int _elementIndex;

		public ArrayRemoveHandler()
		{
			_editor = null!;
		}

		public ArrayRemoveHandler(VariantResolverEditor editor, int elementIndex)
		{
			_editor = editor;
			_elementIndex = elementIndex;
		}

		public void HandlePressed()
		{
			_editor.OnRemoveElement(_elementIndex);
		}
	}
}
#endif
