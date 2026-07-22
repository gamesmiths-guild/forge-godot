// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Statescript.Nodes.State;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class AbilityEndListenerNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.State.AbilityEndListenerNode";

	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		return outputIndex == AbilityEndListenerNode.AbilityOutput ? "AbilityHandle" : null;
	}
}
#endif
