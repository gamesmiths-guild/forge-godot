// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

[Tool]
internal sealed partial class SetEffectLevelNodeEditor : StandardNodeEditorBase
{
	private const string OperationConfigKey = "operation";

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.Action.SetEffectLevelNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams =>
	[
		new NodeConfigParam(
			OperationConfigKey,
			"Operation",
			[nameof(SetEffectLevelOperation.LevelUp), nameof(SetEffectLevelOperation.SetLevel)],
			DefaultName: nameof(SetEffectLevelOperation.LevelUp),
			AffectsLayout: true),
	];

	// The runtime node only reads the Level input for the SetLevel operation; LevelUp just increments, so the Level row
	// is hidden there to avoid showing a value that is never used.
	protected override bool IsInputVisible(int inputIndex)
	{
		if (inputIndex == SetEffectLevelNode.LevelInput)
		{
			return ReadStringConfig(OperationConfigKey, nameof(SetEffectLevelOperation.LevelUp))
				== nameof(SetEffectLevelOperation.SetLevel);
		}

		return true;
	}
}
#endif
