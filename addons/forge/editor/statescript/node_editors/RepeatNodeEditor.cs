// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Statescript.Nodes.State;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Custom node editor for the <c>RepeatNode</c>. Everything renders through the standard rows; the only reason this
/// editor exists is the condition seed.
/// </summary>
[Tool]
internal sealed partial class RepeatNodeEditor : StandardNodeEditorBase
{
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.State.RepeatNode";

	protected override Variant? GetDefaultInputConstant(int inputIndex)
	{
		// The loop's condition means "keep going", so its conventional default is true. Left at the bool zero value a
		// freshly dropped node would be seeded with a constant false and silently run no iterations at all.
		return inputIndex == RepeatNode.ConditionInput ? true : null;
	}
}
#endif
