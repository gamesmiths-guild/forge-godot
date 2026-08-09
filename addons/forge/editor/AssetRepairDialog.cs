// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Shows what an asset repair would remove, and performs it once confirmed.
/// </summary>
/// <remarks>
/// Owned by the plugin and shared by every entry point - the Tools menu and the Tags dock - so there is one repair
/// flow rather than one per button.
/// </remarks>
[Tool]
public sealed partial class AssetRepairDialog : ConfirmationDialog
{
	private const int MaxListedFindings = 40;

	// Not AppendLine: on Windows that writes "\r\n", and Godot's label treats the carriage return as a break of its
	// own, which double-spaces every line.
	private const char LineBreak = '\n';

	private static readonly Vector2I _dialogSize = new(700, 300);

	public override void _Ready()
	{
		base._Ready();

		Title = "Repair Assets Tags";
		OkButtonText = "Repair";

		// Connected by name rather than by C# event: a delegate-backed connection is dropped on assembly reload, and
		// this dialog outlives several of those.
		Connect(AcceptDialog.SignalName.Confirmed, new Callable(this, MethodName.OnConfirmed));
	}

	/// <summary>
	/// Scans the project and opens, reporting what a repair would change.
	/// </summary>
	public void Open()
	{
		// Scan first and show what would change: a repair rewrites assets across the whole project, so it must never
		// be a single unconfirmed click.
		List<AssetRepairTool.RepairFinding> findings = AssetRepairTool.Scan();

		if (findings.Count == 0)
		{
			DialogText = "Every tag reference in this project resolves. Nothing to repair.";
			GetOkButton().Visible = false;
			CancelButtonText = "Close";
		}
		else
		{
			DialogText = BuildPreview(findings);
			GetOkButton().Visible = true;
			CancelButtonText = "Cancel";
		}

		// An explicit size is required, not just polite: MinSize is a floor, so a long findings list would otherwise
		// grow the dialog past the screen and push its own buttons out of reach.
		PopupCentered(_dialogSize);
	}

	private static string BuildPreview(List<AssetRepairTool.RepairFinding> findings)
	{
		var builder = new StringBuilder();
		int assetCount = findings.Select(finding => finding.AssetPath).Distinct().Count();

		string summary =
			$"{findings.Count} tag reference(s) across {assetCount} asset(s) do not resolve against the "
			+ "project's registered tags.";

		builder.Append(summary)
			.Append(LineBreak)
			.Append(LineBreak)
			.Append("Repairing removes them and saves the affected assets. This cannot be undone.")
			.Append(LineBreak)
			.Append(LineBreak);

		foreach (IGrouping<string, AssetRepairTool.RepairFinding> asset in findings
			.Take(MaxListedFindings)
			.GroupBy(finding => finding.AssetPath))
		{
			builder.Append(asset.Key).Append(LineBreak);

			foreach (AssetRepairTool.RepairFinding finding in asset)
			{
				builder.Append(CultureInfo.InvariantCulture, $"    {finding.Location}: {finding.Tag}")
					.Append(LineBreak);
			}
		}

		if (findings.Count > MaxListedFindings)
		{
			builder.Append(CultureInfo.InvariantCulture, $"    ... and {findings.Count - MaxListedFindings} more.")
				.Append(LineBreak);
		}

		return builder.ToString();
	}

	// Must stay an instance method: Godot invokes it by name through this dialog's own method table.
#pragma warning disable CA1822, S2325
	private void OnConfirmed()
#pragma warning restore CA1822, S2325
	{
		List<AssetRepairTool.RepairFinding> repaired = AssetRepairTool.Apply();

		GD.Print($"Repaired {repaired.Count} tag reference(s).");
	}
}
#endif
