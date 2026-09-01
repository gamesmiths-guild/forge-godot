// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Instantiate Scene 3D.
/// </summary>
[Tool]
internal sealed partial class InstantiateScene3DNodeEditor : SceneNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action.InstantiateScene3DNode";
}
#endif
