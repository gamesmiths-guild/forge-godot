// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors a constant <see cref="Shape3D"/> reference.
/// </summary>
[Tool]
internal sealed partial class ShapePicker3DResolverEditor : ShapeResolverEditorBase3D
{
	private Shape3D? _selectedShape;

	public override string DisplayName => "Constant";

	public override string ResolverTypeId => "ShapePicker3D";

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _selectedShape is null ? "Shape" : _selectedShape.GetType().Name;
		return true;
	}

	protected override void BuildShapeRows(VBoxContainer root, ShapeResolverResourceBase3D? existingResource)
	{
		_selectedShape = (existingResource as ShapePicker3DResolverResource)?.Shape;

		var picker = new EditorResourcePicker
		{
			BaseType = nameof(Shape3D),
			EditedResource = _selectedShape,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "A shape authored as a resource. Its dimensions are fixed and cannot scale at runtime.",
		};

		picker.ResourceChanged += resource =>
		{
			_selectedShape = resource as Shape3D;
			NotifyChanged();
		};

		root.AddChild(CreateRow("Shape:", picker));
	}

	protected override ShapeResolverResourceBase3D BuildResource()
	{
		return new ShapePicker3DResolverResource { Shape = _selectedShape };
	}
}
#endif
