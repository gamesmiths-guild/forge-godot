// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Track Target 2D.
/// </summary>
[Tool]
internal sealed partial class TrackTarget2DNodeEditor : StandardNodeEditorBase
{
	// Input property index of the turn rate, matching TrackTarget2DNode.
	private const int SpeedInputIndex = 2;

	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%Turret"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.TrackTarget2DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	protected override string? GetInputLabel(int inputIndex)
	{
		// Radians per second rather than degrees, matching Rotate To 2D: the turn is measured in the angles core's own
		// resolvers produce, and Deg To Rad is how a degree figure gets there.
		return inputIndex == SpeedInputIndex ? "Speed (rad/s)" : null;
	}
}
#endif
