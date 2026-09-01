// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Collision Override 2D.
/// </summary>
[Tool]
internal sealed partial class CollisionOverride2DNodeEditor : CollisionBitsNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.CollisionOverride2DNode";
}
#endif
