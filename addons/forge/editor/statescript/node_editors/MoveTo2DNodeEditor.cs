// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for Move To 2D.
/// </summary>
/// <remarks>
/// The mode setting relabels the value input rather than hiding anything, since the input is read either way - it just
/// means seconds or units per second depending on the mode. That relabelling is what
/// <see cref="StandardNodeEditorBase.GetInputLabel"/> exists for, and it is why the mode is declared as affecting
/// layout: without the rebuild the row would keep whichever label it was first drawn with.
/// </remarks>
[Tool]
internal sealed partial class MoveTo2DNodeEditor : StandardNodeEditorBase
{
	// Input property index of the duration or speed, matching MoveTo2DNode.
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
		new NodeConfigParam("easing", "Easing", SpatialSettingNames.Easings, DefaultName: "Linear"),
		new NodeConfigParam("nodePath", "Node", IsText: true, Placeholder: "%TargetPoint"),
	];

	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.MoveTo2DNode";

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
}
#endif
