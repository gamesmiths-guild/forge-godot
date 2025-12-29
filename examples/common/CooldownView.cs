using Godot;
using System;

public partial class CooldownView : Control
{
	[Export]
	public ProgressBar CooldownProgressBar { get; private set; }

	[Export]
	public Label CooldownLabel { get; private set; }

	[Export]
	public Label CostLabel { get; private set; }

	[Export]
	public Label TagsLabel { get; private set; }

	public void UpdateCooldown(float cooldownRemaining, float totalCooldown)
	{
		if (totalCooldown <= 0f)
		{
			CooldownProgressBar.Value = 0f;
			CooldownLabel.Text = "Ready";
			return;
		}

		CooldownProgressBar.Value = (float)((totalCooldown - cooldownRemaining) / totalCooldown * 100f);
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
