// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class TryCommitAbilityNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.Condition.TryCommitAbilityNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams =>
	[
		new NodeConfigParam(
			"operation",
			"Commit",
			["CostAndCooldown", "CooldownOnly", "CostOnly"],
			DefaultName: "CostAndCooldown"),
	];
}
#endif
