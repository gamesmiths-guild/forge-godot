// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Overlap 3D.
/// </summary>
/// <remarks>
/// The two source modes take disjoint configuration, so the rows follow the mode: an existing area has no shape to
/// place, and a shape the query builds has no area to point at — nor an entity, since the entity operand exists only to
/// say whose area to read.
/// </remarks>
[Tool]
internal sealed partial class Overlap3DNodeEditor : StandardNodeEditorBase
{
	// Input property indexes, matching Overlap3DNode. The entity names whose area to read and so belongs to the
	// existing-area mode; the four below it place a shape the query builds and belong to the other.
	private const int EntityInputIndex = 0;
	private const int ShapeInputIndex = 1;
	private const int PositionInputIndex = 2;
	private const int RotationInputIndex = 3;
	private const int MaskInputIndex = 4;
	private const int IgnoreInputIndex = 6;

	private const string SourceModeKey = "sourceMode";
	private const string AreaPathKey = "areaPath";
	private const string ExistingAreaMode = "ExistingArea";

	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam(
			SourceModeKey,
			"Source",
			PhysicsSettingNames.OverlapSources,
			DefaultName: ExistingAreaMode,
			AffectsLayout: true),
		new NodeConfigParam(AreaPathKey, "Area", IsText: true, Placeholder: "%WeaponHitbox"),
		new NodeConfigParam("includeAreas", "Include Areas", DefaultBool: false),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.Overlap3DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	private bool IsExistingArea => ReadStringConfig(SourceModeKey, ExistingAreaMode) == ExistingAreaMode;

	protected override StatescriptResolverResource? GetDefaultInputResolver(int inputIndex)
	{
		// The ignore row starts as the caster, in both modes: a watch is almost never meant to report the entity
		// running it. The position and the shape start on an Entity Position 3D and a sphere, matching the resolver -
		// and the shape has to, because an unbound one is not a query that finds nothing, it is a query that never
		// runs, and the poll returns without a word to say so.
		return inputIndex switch
		{
			IgnoreInputIndex => EntityIgnoreOperand.BuildOwner(),
			PositionInputIndex => new EntityPosition3DResolverResource(),
			ShapeInputIndex => new SphereShape3DResolverResource(),
			_ => null,
		};
	}

	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		return outputIndex == 0 ? "Entity" : null;
	}

	protected override bool IsSettingVisible(string key)
	{
		return key != AreaPathKey || IsExistingArea;
	}

	protected override bool IsInputVisible(int inputIndex)
	{
		if (IsExistingArea)
		{
			return inputIndex is not (ShapeInputIndex or PositionInputIndex or RotationInputIndex or MaskInputIndex);
		}

		return inputIndex != EntityInputIndex;
	}
}
#endif
