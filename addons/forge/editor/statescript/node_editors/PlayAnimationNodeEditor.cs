// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Play Animation.
/// </summary>
[Tool]
internal sealed partial class PlayAnimationNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("playerPath", "Player", IsText: true, Placeholder: "%AnimationPlayer"),
		new NodeConfigParam("animation", "Animation", IsText: true, Placeholder: "attack"),
		new NodeConfigParam("stopOnDeactivate", "Stop On Deactivate", DefaultBool: true),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.PlayAnimationNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
