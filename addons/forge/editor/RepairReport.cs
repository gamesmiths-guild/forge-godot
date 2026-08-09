// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// The outcome of a repair scan or repair.
/// </summary>
/// <param name="Findings">The dangling tag references found, or the ones actually repaired.</param>
/// <param name="SkippedAssets">
/// Assets that could not be inspected at all. Reported alongside the findings so an empty result is never presented
/// as an all-clear when part of the project was never looked at.
/// </param>
internal readonly record struct RepairReport(
	List<AssetRepairTool.RepairFinding> Findings,
	List<string> SkippedAssets);
#endif
