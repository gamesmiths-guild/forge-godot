// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Play Audio One Shot.
/// </summary>
[Tool]
internal sealed partial class PlayAudioOneShotNodeEditor : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("playerPath", "Player", IsText: true, Placeholder: "%CastAudio"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.PlayAudioOneShotNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
