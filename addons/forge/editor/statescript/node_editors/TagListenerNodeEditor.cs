// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Statescript.Nodes.State;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class TagListenerNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.State.TagListenerNode";

	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		return outputIndex == TagListenerNode.TagOutput ? "Tag" : null;
	}
}
#endif
