// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Play Animation One Shot.
/// </summary>
[Tool]
internal sealed partial class PlayAnimationOneShotNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("playerPath", "Player", IsText: true, Placeholder: "%AnimationPlayer"),
		new NodeConfigParam("animation", "Animation", IsText: true, Placeholder: "attack"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.PlayAnimationOneShotNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
