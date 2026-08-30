// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Input Action.
/// </summary>
[Tool]
internal sealed partial class InputActionNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam(
			"actionName",
			"Action",
			IsText: true,
			Placeholder: "action_name",
			SuggestsInputActions: true),
		new NodeConfigParam("deactivateOnPressed", "Deactivate On Pressed", DefaultBool: false),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.InputActionNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
