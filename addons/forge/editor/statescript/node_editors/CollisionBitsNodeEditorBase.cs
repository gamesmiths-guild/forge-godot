// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Shared editor for the two collision bits nodes, which are configured identically and differ only in whether the
/// change is put back.
/// </summary>
internal abstract partial class CollisionBitsNodeEditorBase : StandardNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam(
			"target",
			"Field",
			PhysicsSettingNames.CollisionTargets,
			DefaultName: "Layer"),
		new NodeConfigParam(
			"operation",
			"Operation",
			PhysicsSettingNames.CollisionOperations,
			DefaultName: "Clear"),
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%Body"),
	];

	/// <inheritdoc/>
	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;
}
#endif
