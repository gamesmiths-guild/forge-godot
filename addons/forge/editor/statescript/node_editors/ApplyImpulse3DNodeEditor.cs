// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Apply Impulse 3D.
/// </summary>
[Tool]
internal sealed partial class ApplyImpulse3DNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%Body"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.ApplyImpulse3DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
