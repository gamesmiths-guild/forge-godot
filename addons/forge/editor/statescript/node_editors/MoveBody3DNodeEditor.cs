// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Move Body 3D.
/// </summary>
/// <remarks>
/// The mode setting relabels the value input rather than hiding anything, exactly as Move To 3D's does: the input is
/// read either way and only means seconds or units per second depending on the mode, which is what
/// <see cref="StandardNodeEditorBase.GetInputLabel"/> exists for and why the mode is declared as affecting layout.
/// </remarks>
[Tool]
internal sealed partial class MoveBody3DNodeEditor : StandardNodeEditorBase
{
	// Input property index of the duration or speed, matching MoveBody3DNode.
	private const int ValueInputIndex = 2;

	private const string ModeKey = "mode";
	private const string DurationMode = "Duration";

	private static readonly IReadOnlyList<NodeConfigParam> _configParams =
	[
		new NodeConfigParam(
			ModeKey,
			"Mode",
			SpatialSettingNames.MoveModes,
			DefaultName: DurationMode,
			AffectsLayout: true),
		new NodeConfigParam(
			"blocked",
			"When Blocked",
			SpatialSettingNames.BlockedResponses,
			DefaultName: "Stop"),
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%Body"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.MoveBody3DNode";

	protected override IReadOnlyList<NodeConfigParam> ConstructorParams => _configParams;

	private bool IsDuration => ReadStringConfig(ModeKey, DurationMode) == DurationMode;

	protected override string? GetInputLabel(int inputIndex)
	{
		if (inputIndex != ValueInputIndex)
		{
			return null;
		}

		return IsDuration ? "Duration (s)" : "Speed (units/s)";
	}

	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		return outputIndex switch
		{
			0 => "Entity",
			1 => "GodotNode",
			_ => null,
		};
	}
}
#endif
