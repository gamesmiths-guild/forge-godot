// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Node Enabled Override.
/// </summary>
[Tool]
internal sealed partial class NodeEnabledOverrideNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam(
			"aspect",
			"Aspect",
			["Visible", "Processing", "PhysicsProcessing", "Monitoring", "Monitorable"],
			DefaultName: "Visible"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.NodeEnabledOverrideNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
