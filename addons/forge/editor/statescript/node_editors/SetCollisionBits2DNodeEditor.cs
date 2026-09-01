// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Set Collision Bits 2D.
/// </summary>
[Tool]
internal sealed partial class SetCollisionBits2DNodeEditor : CollisionBitsNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.SetCollisionBits2DNode";
}
#endif
