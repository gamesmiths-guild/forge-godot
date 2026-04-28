// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class ScalarConstantResolverEditorBase<TResource> : NodeEditorProperty
	where TResource : TypedConstantResolverResourceBase, new()
{
	private StatescriptVariableType _valueType;
	private bool _allowTypeSelection;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		if (property?.Resolver is TResource existing)
		{
			_valueType = NormalizeValueType(existing.ValueType);
		}
		else if (expectedType == typeof(double))
		{
			_valueType = StatescriptVariableType.Double;
		}
		else
		{
			_valueType = StatescriptVariableType.Float;
		}

		_allowTypeSelection = expectedType == typeof(ForgeVariant128);

		if (!_allowTypeSelection)
		{
			_valueType = expectedType == typeof(double)
				? StatescriptVariableType.Double
				: StatescriptVariableType.Float;
		}

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(row);

		row.AddChild(new Label
		{
			Text = "Type:",
			CustomMinimumSize = new Vector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		if (_allowTypeSelection)
		{
			var typeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			typeDropdown.AddItem("Float");
			typeDropdown.AddItem("Double");
			typeDropdown.Selected = _valueType == StatescriptVariableType.Double ? 1 : 0;
			typeDropdown.ItemSelected += index =>
				_valueType = index == 1 ? StatescriptVariableType.Double : StatescriptVariableType.Float;
			row.AddChild(typeDropdown);
		}
		else
		{
			row.AddChild(new Label
			{
				Text = _valueType == StatescriptVariableType.Double ? "Double" : "Float",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			});
		}
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new TResource { ValueType = _valueType };
	}

	private static StatescriptVariableType NormalizeValueType(StatescriptVariableType valueType)
	{
		return valueType == StatescriptVariableType.Double
			? StatescriptVariableType.Double
			: StatescriptVariableType.Float;
	}
}
#endif
