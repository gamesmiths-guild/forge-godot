// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Rotate To 2D.
/// </summary>
/// <remarks>
/// The mode setting relabels the value input rather than hiding anything, the same as Move To: the input is read
/// either way and just means seconds or radians per second depending on the mode.
/// </remarks>
[Tool]
internal sealed partial class RotateTo2DNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("mode", "Mode", SpatialSettingNames.MoveModes, DefaultName: "Duration"),
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%TurretPivot"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.RotateTo2DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
