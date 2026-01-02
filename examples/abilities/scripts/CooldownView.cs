// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class CooldownView : Control
{
	[Export]
	public required ProgressBar CooldownProgressBar { get; set; }

	[Export]
	public required Label CooldownLabel { get; set; }

	[Export]
	public required Label CostLabel { get; set; }

	[Export]
	public required Label TagsLabel { get; set; }

	public void UpdateCooldown(float cooldownRemaining, float totalCooldown)
	{
		if (totalCooldown <= 0f)
		{
			CooldownProgressBar.Value = 0f;
			CooldownLabel.Text = "Ready";
			return;
		}

		CooldownProgressBar.Value = (totalCooldown - cooldownRemaining) / totalCooldown * 100f;
		CooldownLabel.Text = $"{cooldownRemaining:F1}s";
	}

	public void UpdateCost(string costText)
	{
		CostLabel.Text = costText;
	}

	public void UpdateTags(string tagsText)
	{
		TagsLabel.Text = tagsText;
	}
}
