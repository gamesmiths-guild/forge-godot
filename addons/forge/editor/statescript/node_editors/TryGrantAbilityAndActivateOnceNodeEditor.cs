// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class TryGrantAbilityAndActivateOnceNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.Condition.TryGrantAbilityAndActivateOnceNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams =>
	[
		new NodeConfigParam(
			"levelOverridePolicy",
			"Level Override",
			["None", "Equal", "Higher", "Lower"],
			DefaultName: "None"),
	];

	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		return outputIndex == TryGrantAbilityAndActivateOnceNode.AbilityOutput ? "AbilityHandle" : null;
	}

	protected override Variant? GetDefaultInputConstant(int inputIndex)
	{
		return inputIndex == TryGrantAbilityAndActivateOnceNode.LevelInput ? 1 : null;
	}
}
#endif
