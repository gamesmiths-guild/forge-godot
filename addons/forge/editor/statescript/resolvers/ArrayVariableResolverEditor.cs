// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ArrayVariableResolverEditor : NodeEditorProperty
{
	private VariantResolverEditor? _innerEditor;

	public override string DisplayName => "Array";

	public override string ResolverTypeId => "ArrayVariable";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType.IsArray;
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_innerEditor = new VariantResolverEditor();
		Type elementExpectedType = expectedType.IsArray ? expectedType.GetElementType() ?? typeof(int) : expectedType;
		VariantResolverResource? tempResolver = null;

		if (property?.Resolver is ArrayVariableResolverResource existing)
		{
			tempResolver = new VariantResolverResource
			{
				IsArray = true,
				ValueType = existing.ValueType,
				ArrayValues = [.. existing.ArrayValues],
				IsArrayExpanded = existing.IsArrayExpanded,
			};
		}

		StatescriptNodeProperty? tempProperty = tempResolver is null
			? null
			: new StatescriptNodeProperty { Resolver = tempResolver };

		_innerEditor.Setup(graph, tempProperty, elementExpectedType, onChanged, true);
		_innerEditor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		AddChild(_innerEditor);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		if (_innerEditor is null)
		{
			return;
		}

		var tempProperty = new StatescriptNodeProperty();
		_innerEditor.SaveTo(tempProperty);

		if (tempProperty.Resolver is not VariantResolverResource variant)
		{
			return;
		}

		property.Resolver = new ArrayVariableResolverResource
		{
			ValueType = variant.ValueType,
			ArrayValues = [.. variant.ArrayValues],
			IsArrayExpanded = variant.IsArrayExpanded,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_innerEditor?.ClearCallbacks();
	}
}
#endif
