// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Move To 2D.
/// </summary>
/// <remarks>
/// The mode setting relabels the value input rather than hiding anything, since the input is read either way - it just
/// means seconds or units per second depending on the mode.
/// </remarks>
[Tool]
internal sealed partial class MoveTo2DNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("mode", "Mode", SpatialSettingNames.MoveModes, DefaultName: "Duration"),
		new NodeConfigParam("easing", "Easing", SpatialSettingNames.Easings, DefaultName: "Linear"),
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%TargetPoint"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.MoveTo2DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
