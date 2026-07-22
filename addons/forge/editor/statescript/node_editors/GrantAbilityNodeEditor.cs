// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class GrantAbilityNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.State.GrantAbilityNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams =>
	[
		new NodeConfigParam(
			"removalPolicy",
			"On Deactivate",
			["CancelImmediately", "RemoveOnEnd", "Ignore"],
			DefaultName: "CancelImmediately"),
		new NodeConfigParam(
			"levelOverridePolicy",
			"Level Override",
			["None", "Equal", "Higher", "Lower"],
			DefaultName: "None"),
	];

	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		return outputIndex == GrantAbilityNode.AbilityOutput ? "AbilityHandle" : null;
	}

	protected override Variant? GetDefaultInputConstant(int inputIndex)
	{
		return inputIndex == GrantAbilityNode.LevelInput ? 1 : null;
	}
}
#endif
