// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class ConditionMonitorNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.State.ConditionMonitorNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams =>
	[
		new NodeConfigParam("deactivateWhenTrue", "Deactivate When True", DefaultBool: false),
		new NodeConfigParam("initialCheckOnActivate", "Initial Check On Activate", DefaultBool: true),
	];
}
#endif
