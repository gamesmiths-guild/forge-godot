// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class OrderByResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _keySelectorPicker;
	private SortDirection _direction = SortDirection.Ascending;

	public override string DisplayName => "Order By";

	public override string ResolverTypeId => "OrderBy";

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new OrderByResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			KeySelector = _keySelectorPicker?.BuildResource(),
			KeySelectorFolded = _keySelectorPicker?.Folded ?? true,
			Direction = _direction,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_keySelectorPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (base.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		return _keySelectorPicker is not null && _keySelectorPicker.TryGetHighlightedVariableName(out variableName);
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as OrderByResolverResource;
		_direction = existingResource?.Direction ?? SortDirection.Ascending;

		_keySelectorPicker = AddOperandPicker(
			root,
			graph,
			existingResource?.KeySelector,
			"Key:",
			[typeof(int), typeof(float), typeof(double)],
			existingResource?.KeySelectorFolded ?? true,
			onChanged,
			beginsIterationScope: true);

		var directionDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		directionDropdown.AddItem("Ascending");
		directionDropdown.AddItem("Descending");
		directionDropdown.Selected = (int)_direction;
		directionDropdown.ItemSelected += index =>
		{
			_direction = (SortDirection)(int)index;
			onChanged();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Order:", directionDropdown, 66.0f));
	}
}
#endif
