// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors a constant <see cref="Shape2D"/> reference.
/// </summary>
[Tool]
internal sealed partial class ShapePicker2DResolverEditor : ShapeResolverEditorBase2D
{
	private Shape2D? _selectedShape;

	public override string DisplayName => "Constant";

	public override string ResolverTypeId => "ShapePicker2D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _selectedShape is null ? "Shape" : _selectedShape.GetType().Name;
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase2D? existingResource)
	{
		_selectedShape = (existingResource as ShapePicker2DResolverResource)?.Shape;

		var picker = new EditorResourcePicker
		{
			BaseType = nameof(Shape2D),
			EditedResource = _selectedShape,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "A shape authored as a resource. Its dimensions are fixed and cannot scale at runtime.",
		};

		picker.ResourceChanged += resource =>
		{
			_selectedShape = resource as Shape2D;
			NotifyChanged();
		};

		root.AddChild(CreateRow("Shape:", picker));
	}

	protected override ShapeResolverResourceBase2D BuildResource()
	{
		return new ShapePicker2DResolverResource { Shape = _selectedShape };
	}
}
#endif
