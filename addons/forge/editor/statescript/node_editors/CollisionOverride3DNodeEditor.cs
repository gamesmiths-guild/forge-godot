// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Collision Override 3D.
/// </summary>
[Tool]
internal sealed partial class CollisionOverride3DNodeEditor : CollisionBitsNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.CollisionOverride3DNode";
}
#endif
