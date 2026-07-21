// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class GrantAbilityPermanentlyNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.Action.GrantAbilityPermanentlyNode";

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
		return outputIndex == GrantAbilityPermanentlyNode.AbilityOutput ? "AbilityHandle" : null;
	}

	protected override Variant? GetDefaultInputConstant(int inputIndex)
	{
		return inputIndex == GrantAbilityPermanentlyNode.LevelInput ? 1 : null;
	}
}
#endif
