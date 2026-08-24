// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Raycast 3D.
/// </summary>
[Tool]
internal sealed partial class Raycast3DNodeEditor : RaycastNodeEditorBase
{
	// Input property index of the ignore array, matching RaycastNodeParameters3D.
	private const int IgnoreInputIndex = 4;

	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("collideWithAreas", "Hit Areas", DefaultBool: false),
		new NodeConfigParam("hitFromInside", "Hit From Inside", DefaultBool: false),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Condition.Raycast3DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	protected override StatescriptResolverResource? GetDefaultInputResolver(int inputIndex)
	{
		// The ignore row starts as the caster: a ray fired from the caster's own position starts on its own collider.
		return inputIndex == IgnoreInputIndex ? EntityIgnoreOperand.BuildOwner() : null;
	}
}
#endif
