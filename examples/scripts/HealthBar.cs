// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core.Godot;
using Godot;

public partial class HealthBar : ProgressBar
{
	[Export]
	public required Label HealthBarLabel { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ForgeEntity forgeEntity = GetParent().GetNode<ForgeEntity>("%Forge Entity");

		GD.Print("found");
		Gamesmiths.Forge.Core.Attribute healthAttribute = forgeEntity.Attributes["CharacterAttributes.Health"];
		healthAttribute.OnValueChanged += HealthBar_OnValueChanged;
		Value = (float)healthAttribute.CurrentValue / healthAttribute.Max * 100;
		HealthBarLabel.Text = $"{healthAttribute.CurrentValue}/{healthAttribute.Max}";
	}

	private void HealthBar_OnValueChanged(Gamesmiths.Forge.Core.Attribute healthAttribute, int change)
	{
		Value = (float)healthAttribute.CurrentValue / healthAttribute.Max * 100;
		HealthBarLabel.Text = $"{healthAttribute.CurrentValue}/{healthAttribute.Max}";
	}
}
