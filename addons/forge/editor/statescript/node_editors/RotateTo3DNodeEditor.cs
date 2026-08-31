// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Rotate To 3D.
/// </summary>
/// <remarks>
/// The mode setting relabels the value input rather than hiding anything, the same as Move To: the input is read
/// either way and just means seconds or radians per second depending on the mode.
/// </remarks>
[Tool]
internal sealed partial class RotateTo3DNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("mode", "Mode", SpatialSettingNames.MoveModes, DefaultName: "Duration"),
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%TurretYaw"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.RotateTo3DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
