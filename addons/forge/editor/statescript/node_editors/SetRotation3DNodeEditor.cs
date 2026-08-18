// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Set Rotation 3D.
/// </summary>
[Tool]
internal sealed partial class SetRotation3DNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("space", "Space", SpatialSettingNames.Spaces, DefaultName: "Global"),
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%CastPoint"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.SetRotation3DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
