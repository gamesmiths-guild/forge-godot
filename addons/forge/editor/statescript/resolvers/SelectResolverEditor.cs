// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SelectResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _projectionPicker;

	public override string DisplayName => "Select (Map)";

	public override string ResolverTypeId => "Select";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new SelectResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Projection = _projectionPicker?.BuildResource(),
			ProjectionFolded = _projectionPicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_projectionPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (base.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		return _projectionPicker is not null && _projectionPicker.TryGetHighlightedVariableName(out variableName);
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		// The projected element type does not constrain the source: entities can project into values.
		return [typeof(ForgeVariant128), typeof(IForgeEntity)];
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as SelectResolverResource;
		_projectionPicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Projection,
			"Project:",
			GetAllowedExpectedTypes(expectedType),
			existingResource?.ProjectionFolded ?? true,
			onChanged,
			isArray: false,
			beginsIterationScope: true);
	}
}
#endif
