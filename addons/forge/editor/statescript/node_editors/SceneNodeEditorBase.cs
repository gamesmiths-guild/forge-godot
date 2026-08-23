// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Shared editor for the scene nodes, which differ in what they own rather than in how they are configured.
/// </summary>
internal abstract partial class SceneNodeEditorBase : StandardNodeEditorBase
{
	// Input property indexes of the two parent operands, adjacent and last so the row holds its place when the mode
	// changes. The same on both scene nodes.
	private const int ParentEntityInputIndex = 3;
	private const int ParentNodeInputIndex = 4;

	private const string ParentModeKey = "parentMode";
	private const string CurrentSceneMode = "CurrentScene";
	private const string EntityMode = "Entity";
	private const string NodeMode = "Node";

	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam(
			ParentModeKey,
			"Parent",
			[CurrentSceneMode, EntityMode, NodeMode],
			DefaultName: CurrentSceneMode,
			AffectsLayout: true),
		new NodeConfigParam("passOwnership", "Pass ownership", DefaultBool: true),
	];

	/// <inheritdoc/>
	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	/// <inheritdoc/>
	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		// Both outputs are object-lane, so they have to bind through their object variable type rather than the default
		// value-lane path.
		return outputIndex switch
		{
			0 => "GodotNode",
			1 => "Entity",
			_ => null,
		};
	}

	/// <inheritdoc/>
	protected override bool IsInputVisible(int inputIndex)
	{
		// Each parent operand only names a parent in its own mode; showing one otherwise invites authoring a parent
		// that is silently ignored. Current-scene mode shows neither, and places the instance on the caster unless a
		// position is bound.
		string parentMode = ReadStringConfig(ParentModeKey, CurrentSceneMode);

		return inputIndex switch
		{
			ParentEntityInputIndex => parentMode == EntityMode,
			ParentNodeInputIndex => parentMode == NodeMode,
			_ => true,
		};
	}
}
#endif
