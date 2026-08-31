// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Sweep 2D.
/// </summary>
[Tool]
internal sealed partial class Sweep2DNodeEditor : ShapecastNodeEditorBase
{
	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam("collideWithAreas", "Hit Areas", DefaultBool: false),
		new NodeConfigParam("oneShot", "One Shot", DefaultBool: false),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.Sweep2DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	protected override StatescriptResolverResource BuildDefaultShape()
	{
		return new CircleShape2DResolverResource();
	}
}
#endif
