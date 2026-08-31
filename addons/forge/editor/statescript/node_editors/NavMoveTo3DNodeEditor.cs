// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Nav Move To 3D.
/// </summary>
[Tool]
internal sealed partial class NavMoveTo3DNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("agentPath", "Agent", IsText: true, Placeholder: "NavigationAgent3D"),
		new NodeConfigParam("useSafeVelocity", "Use Safe Velocity", DefaultBool: false),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.NavMoveTo3DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
