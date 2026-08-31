// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Emit Signal.
/// </summary>
[Tool]
internal sealed partial class EmitSignalNodeEditor : InteropArgumentNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("signalName", "Signal", IsText: true, Placeholder: "dashed"),
		new NodeConfigParam(
			Argument1TypeKey,
			"Arg 1",
			InteropValueTypeNames.Arguments,
			DefaultName: NoneName,
			AffectsLayout: true,
			RetypesInput: EmitSignalNode.Argument1Input),
		new NodeConfigParam(
			Argument2TypeKey,
			"Arg 2",
			InteropValueTypeNames.Arguments,
			DefaultName: NoneName,
			AffectsLayout: true,
			RetypesInput: EmitSignalNode.Argument2Input),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.EmitSignalNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	protected override int Argument1InputIndex => 1;
}
#endif
