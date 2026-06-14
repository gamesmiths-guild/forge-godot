// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class CueNodeEditor : CueNodeEditorBase
{
	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.State.CueNode";
}
#endif
