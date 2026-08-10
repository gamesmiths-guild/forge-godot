// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class TryRevokeAbilityNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.Condition.TryRevokeAbilityNode";

	// Ignore is deliberately not offered for removalPolicy: the core revocation API rejects it, since a revocation
	// that ignores its own request does nothing.
	protected override IReadOnlyList<NodeConfigParam> ConstructorParams =>
	[
		new NodeConfigParam(
			"scope",
			"Revoke",
			["PermanentGrants", "AllGrants"],
			DefaultName: "PermanentGrants"),
		new NodeConfigParam(
			"removalPolicy",
			"On Revoke",
			["CancelImmediately", "RemoveOnEnd"],
			DefaultName: "CancelImmediately"),
	];
}
#endif
