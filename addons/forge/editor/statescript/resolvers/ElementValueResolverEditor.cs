// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ElementValueResolverEditor : NodeEditorProperty
{
	private static readonly StatescriptVariableType[] _selectableTypes =
	[
		StatescriptVariableType.Bool,
		StatescriptVariableType.Int,
		StatescriptVariableType.Float,
		StatescriptVariableType.Double,
		StatescriptVariableType.Vector2,
		StatescriptVariableType.Vector3,
		StatescriptVariableType.Vector4,
		StatescriptVariableType.Plane,
		StatescriptVariableType.Quaternion,
	];

	private StatescriptVariableType _selectedType = StatescriptVariableType.Int;
	private Action? _onChanged;

	public override string DisplayName => "Element Value";

	public override string ResolverTypeId => "ElementValue";

	public override bool RequiresIterationScope => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(Variant128) || TryGetExactVariableType(expectedType, out _);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		bool hasContextType = TryGetExactVariableType(expectedType, out StatescriptVariableType contextType);

		if (property?.Resolver is ElementValueResolverResource existingResource)
		{
			_selectedType = existingResource.ValueType;
		}

		if (hasContextType)
		{
			_selectedType = contextType;
		}

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);
		root.AddChild(new Label { Text = "Reads the iterated array element." });

		if (hasContextType)
		{
			return;
		}

		var typeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		int selectedIndex = 0;

		for (int i = 0; i < _selectableTypes.Length; i++)
		{
			typeDropdown.AddItem(_selectableTypes[i].ToString());

			if (_selectableTypes[i] == _selectedType)
			{
				selectedIndex = i;
			}
		}

		typeDropdown.Selected = selectedIndex;
		typeDropdown.TooltipText = "Must match the iterated array's element type.";
		typeDropdown.ItemSelected += OnTypeChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Type:", typeDropdown, 66.0f));
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ElementValueResolverResource { ValueType = _selectedType };
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = $"Element ({_selectedType})";
		return true;
	}

	private static bool TryGetExactVariableType(Type type, out StatescriptVariableType variableType)
	{
		Dictionary<Type, StatescriptVariableType> exactTypes = new()
		{
			[typeof(bool)] = StatescriptVariableType.Bool,
			[typeof(int)] = StatescriptVariableType.Int,
			[typeof(float)] = StatescriptVariableType.Float,
			[typeof(double)] = StatescriptVariableType.Double,
			[typeof(System.Numerics.Vector2)] = StatescriptVariableType.Vector2,
			[typeof(System.Numerics.Vector3)] = StatescriptVariableType.Vector3,
			[typeof(System.Numerics.Vector4)] = StatescriptVariableType.Vector4,
			[typeof(System.Numerics.Plane)] = StatescriptVariableType.Plane,
			[typeof(System.Numerics.Quaternion)] = StatescriptVariableType.Quaternion,
		};

		return exactTypes.TryGetValue(type, out variableType);
	}

	private void OnTypeChanged(long index)
	{
		_selectedType = _selectableTypes[(int)index];
		_onChanged?.Invoke();
	}
}
#endif
