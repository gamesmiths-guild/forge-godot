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
		RepairReport report = AssetRepairTool.Scan();

		if (report.Findings.Count == 0)
		{
			DialogText = DescribeNothingFound(report.SkippedAssets);
			GetOkButton().Visible = false;
			CancelButtonText = "Close";
		}
		else
		{
			DialogText = BuildPreview(report);
			GetOkButton().Visible = true;
			CancelButtonText = "Cancel";
		}

		// An explicit size is required, not just polite: MinSize is a floor, so a long findings list would otherwise
		// grow the dialog past the screen and push its own buttons out of reach.
		PopupCentered(_dialogSize);
	}

	/// <summary>
	/// Describes an empty result, qualified by anything the scan could not look at.
	/// </summary>
	/// <param name="skippedAssets">Assets that were not inspected.</param>
	/// <returns>The message to show.</returns>
	/// <remarks>
	/// A bare all-clear would be untrue when part of the project was never read, and that is exactly the case someone
	/// would go on to trust.
	/// </remarks>
	private static string DescribeNothingFound(List<string> skippedAssets)
	{
		if (skippedAssets.Count == 0)
		{
			return "Every tag reference in this project resolves. Nothing to repair.";
		}

		var builder = new StringBuilder();

		builder.Append("Every tag reference that could be read resolves. Nothing to repair.")
			.Append(LineBreak)
			.Append(LineBreak)
			.Append(CultureInfo.InvariantCulture, $"{skippedAssets.Count} binary asset(s) were not inspected, ")
			.Append("because tags can only be read from text scenes and resources:")
			.Append(LineBreak);

		AppendSkipped(builder, skippedAssets);

		return builder.ToString();
	}

	private static void AppendSkipped(StringBuilder builder, List<string> skippedAssets)
	{
		foreach (string asset in skippedAssets.Take(MaxListedFindings))
		{
			builder.Append(CultureInfo.InvariantCulture, $"    {asset}").Append(LineBreak);
		}

		if (skippedAssets.Count > MaxListedFindings)
		{
			builder.Append(CultureInfo.InvariantCulture, $"    ... and {skippedAssets.Count - MaxListedFindings} more.")
				.Append(LineBreak);
		}
	}

	private static string BuildPreview(RepairReport report)
	{
		List<AssetRepairTool.RepairFinding> findings = report.Findings;
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

		if (report.SkippedAssets.Count > 0)
		{
			builder.Append(LineBreak)
				.Append(CultureInfo.InvariantCulture, $"{report.SkippedAssets.Count} binary asset(s) were not ")
				.Append("inspected and will be left alone:")
				.Append(LineBreak);

			AppendSkipped(builder, report.SkippedAssets);
		}

		return builder.ToString();
	}

	// Must stay an instance method: Godot invokes it by name through this dialog's own method table.
#pragma warning disable CA1822, S2325
	private void OnConfirmed()
#pragma warning restore CA1822, S2325
	{
		RepairReport report = AssetRepairTool.Apply();

		GD.Print($"Repaired {report.Findings.Count} tag reference(s).");
	}
}
#endif
