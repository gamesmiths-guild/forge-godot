// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
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
	private GodotVariant _currentValue;
	private Array<GodotVariant> _arrayValues = [];

	/// <inheritdoc/>
	public override string DisplayName => "Constant";

	/// <inheritdoc/>
	public override Type ValueType => typeof(Variant128);

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
			VBoxContainer arrayEditor = CreateArrayEditor(onChanged);
			AddChild(arrayEditor);
		}
		else
		{
			Control valueEditor = CreateValueEditor(onChanged);
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

	private Control CreateValueEditor(Action onChanged)
	{
		if (_valueType == StatescriptVariableType.Bool)
		{
			return StatescriptEditorControls.CreateBoolEditor(_currentValue.AsBool(), x =>
			{
				_currentValue = GodotVariant.From(x);
				onChanged();
			});
		}

		if (StatescriptEditorControls.IsIntegerType(_valueType)
			|| StatescriptEditorControls.IsFloatType(_valueType))
		{
			return StatescriptEditorControls.CreateNumericSpinSlider(_valueType, _currentValue.AsDouble(), x =>
			{
				_currentValue = GodotVariant.From(x);
				onChanged();
			});
		}

		if (StatescriptEditorControls.IsVectorType(_valueType))
		{
			return StatescriptEditorControls.CreateVectorEditor(
				_valueType,
				x => StatescriptEditorControls.GetVectorComponent(_currentValue, _valueType, x),
				x =>
				{
					_currentValue = StatescriptEditorControls.BuildVectorVariant(_valueType, x);
					onChanged();
				});
		}

		return new Label { Text = _valueType.ToString() };
	}

	private VBoxContainer CreateArrayEditor(Action onChanged)
	{
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		var elementsContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(headerRow);

		var sizeLabel = new Label
		{
			Text = $"Size: {_arrayValues.Count}",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		headerRow.AddChild(sizeLabel);

		Texture2D addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");

		var addButton = new Button
		{
			Icon = addIcon,
			Flat = true,
			TooltipText = "Add Element",
			CustomMinimumSize = new GodotVector2(24, 24),
		};

		addButton.Pressed += () =>
		{
			GodotVariant defaultValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(_valueType);
			_arrayValues.Add(defaultValue);
			onChanged();
			RebuildArrayElements(elementsContainer, sizeLabel, onChanged);
			RaiseLayoutSizeChanged();
		};

		headerRow.AddChild(addButton);

		vBox.AddChild(elementsContainer);

		RebuildArrayElements(elementsContainer, sizeLabel, onChanged);

		return vBox;
	}

	private void RebuildArrayElements(VBoxContainer container, Label sizeLabel, Action onChanged)
	{
		foreach (global::Godot.Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.QueueFree();
		}

		sizeLabel.Text = $"Size: {_arrayValues.Count}";

		Texture2D removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		for (var i = 0; i < _arrayValues.Count; i++)
		{
			var capturedIndex = i;

			if (StatescriptEditorControls.IsVectorType(_valueType))
			{
				var elementVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				container.AddChild(elementVBox);

				var labelRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementVBox.AddChild(labelRow);
				labelRow.AddChild(new Label
				{
					Text = $"[{i}]",
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				});

				AddArrayRemoveButton(labelRow, removeIcon, capturedIndex, container, sizeLabel, onChanged);

				VBoxContainer vectorEditor = StatescriptEditorControls.CreateVectorEditor(
					_valueType,
					x => StatescriptEditorControls.GetVectorComponent(
						_arrayValues[capturedIndex],
						_valueType,
						x),
					x =>
					{
						_arrayValues[capturedIndex] =
							StatescriptEditorControls.BuildVectorVariant(_valueType, x);
						onChanged();
					});

				elementVBox.AddChild(vectorEditor);
			}
			else
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				container.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				if (_valueType == StatescriptVariableType.Bool)
				{
					elementRow.AddChild(StatescriptEditorControls.CreateBoolEditor(
						_arrayValues[capturedIndex].AsBool(),
						x =>
						{
							_arrayValues[capturedIndex] = GodotVariant.From(x);
							onChanged();
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
							onChanged();
						});

					elementRow.AddChild(spin);
				}

				AddArrayRemoveButton(elementRow, removeIcon, capturedIndex, container, sizeLabel, onChanged);
			}
		}
	}

	private void AddArrayRemoveButton(
		HBoxContainer row,
		Texture2D removeIcon,
		int elementIndex,
		VBoxContainer elementsContainer,
		Label sizeLabel,
		Action onChanged)
	{
		var removeButton = new Button
		{
			Icon = removeIcon,
			Flat = true,
			TooltipText = "Remove Element",
			CustomMinimumSize = new GodotVector2(24, 24),
		};

		removeButton.Pressed += () =>
		{
			_arrayValues.RemoveAt(elementIndex);
			onChanged();
			RebuildArrayElements(elementsContainer, sizeLabel, onChanged);
			RaiseLayoutSizeChanged();
		};

		row.AddChild(removeButton);
	}
}
#endif
