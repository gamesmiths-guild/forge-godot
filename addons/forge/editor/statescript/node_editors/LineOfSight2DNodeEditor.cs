// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Line Of Sight 2D.
/// </summary>
[Tool]
internal sealed partial class LineOfSight2DNodeEditor : StandardNodeEditorBase
{
	// Input property index of the ignore array, matching LineOfSight2DNode.
	private const int IgnoreInputIndex = 2;

	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("deactivateOnBlocked", "Deactivate When Blocked", DefaultBool: false),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.LineOfSight2DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	protected override StatescriptResolverResource? GetDefaultInputResolver(int inputIndex)
	{
		// The ignore row starts as the array it almost always is, rather than making every author build the same two
		// entries by hand before the node works at all.
		return inputIndex == IgnoreInputIndex ? EntityIgnoreOperand.BuildOwnerAndTarget() : null;
	}

	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		// The blocker and its collider are object-lane; the position beside them is an ordinary value.
		return outputIndex switch
		{
			0 => "Entity",
			1 => "GodotNode",
			_ => null,
		};
	}
}
#endif
