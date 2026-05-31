// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class ApplyEffectNodeEditor : EffectApplicationNodeEditorBase
{
	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Action.ApplyEffectNode";
}
#endif
