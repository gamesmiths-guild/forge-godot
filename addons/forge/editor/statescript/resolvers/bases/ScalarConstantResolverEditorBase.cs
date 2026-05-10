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
		if (property?.Resolver is TResource)
		{
			_valueType = NormalizeValueType();
		}
		else
		{
			_valueType = StatescriptVariableType.Double;
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

		row.AddChild(new Label
		{
			Text = "Float",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new TResource { ValueType = _valueType };
	}

	private static StatescriptVariableType NormalizeValueType()
	{
		return StatescriptVariableType.Double;
	}
}
#endif
