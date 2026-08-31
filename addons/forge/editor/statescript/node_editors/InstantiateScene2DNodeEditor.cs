// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Instantiate Scene 2D.
/// </summary>
[Tool]
internal sealed partial class InstantiateScene2DNodeEditor : SceneNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.InstantiateScene2DNode";
}
#endif
