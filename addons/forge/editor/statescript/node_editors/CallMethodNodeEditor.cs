// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Call Method.
/// </summary>
[Tool]
internal sealed partial class CallMethodNodeEditor : InteropArgumentNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("methodName", "Method", IsText: true, Placeholder: "open"),
		new NodeConfigParam(
			Argument1TypeKey,
			"Arg 1",
			InteropValueTypeNames.Arguments,
			DefaultName: NoneName,
			AffectsLayout: true,
			RetypesInput: CallMethodNode.Argument1Input),
		new NodeConfigParam(
			Argument2TypeKey,
			"Arg 2",
			InteropValueTypeNames.Arguments,
			DefaultName: NoneName,
			AffectsLayout: true,
			RetypesInput: CallMethodNode.Argument2Input),
		new NodeConfigParam(
			"returnType",
			"Returns",
			InteropValueTypeNames.Arguments,
			DefaultName: NoneName,
			AffectsLayout: true),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.CallMethodNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	protected override int Argument1InputIndex => 1;

	/// <inheritdoc/>
	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		// The return is only declared at all when a type is chosen, so there is one output or none; it binds through
		// the object variable type when that type is a node, and through the value lane otherwise.
		return ReadStringConfig("returnType", NoneName) == "Node" ? "GodotNode" : null;
	}
}
#endif
