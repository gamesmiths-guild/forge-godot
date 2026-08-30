// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Set Node Property.
/// </summary>
/// <remarks>
/// Both the type and the shape rebuild the node, because the value row's own editor is chosen from the type its input
/// declares - which is exactly what these two settings decide.
/// </remarks>
[Tool]
internal sealed partial class SetNodePropertyNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("propertyPath", "Property", IsText: true, Placeholder: "visible"),
		new NodeConfigParam(
			"valueType",
			"Type",
			InteropValueTypeNames.Values,
			DefaultName: "Float",
			AffectsLayout: true),
		new NodeConfigParam("isArray", "Array", AffectsLayout: true),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.SetNodePropertyNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
