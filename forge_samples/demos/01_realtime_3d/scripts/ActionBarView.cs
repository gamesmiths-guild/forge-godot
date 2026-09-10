// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class ActionBarView : Control
{
	[Export]
	public required TextureProgressBar CooldownProgressBar { get; set; }

	[Export]
	public required Label CooldownLabel { get; set; }

	[Export]
	public required Label CostLabel { get; set; }

	[Export]
	public required Label KeyLabel { get; set; }

	[Export]
	public required string KeyText { get; set; }

	public override void _Ready()
	{
		base._Ready();
		KeyLabel.Text = KeyText;
	}

	public void UpdateCooldown(float cooldownRemaining, float totalCooldown)
	{
		if (totalCooldown <= 0f)
		{
			CooldownProgressBar.Value = 0f;
			CooldownLabel.Text = "Ready";
			return;
		}

		CooldownProgressBar.Value = 100f - ((totalCooldown - cooldownRemaining) / totalCooldown * 100f);
		CooldownLabel.Text = $"{cooldownRemaining:F1}s";
	}

	public void UpdateCost(string costText)
	{
		CostLabel.Text = costText;
	}

	public void UpdateActive(bool active)
	{
		KeyLabel.AddThemeColorOverride("font_color", active ? Colors.Yellow : Colors.Black);
	}
}
