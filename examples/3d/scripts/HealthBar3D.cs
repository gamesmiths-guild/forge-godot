// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class HealthBar3D : ProgressBar
{
	[Export]
	public required Label HealthBarLabel { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ForgeEntity forgeEntity = GetParent().GetNode<ForgeEntity>("%Forge Entity");

		EntityAttribute healthAttribute = forgeEntity.Attributes["CharacterAttributes.Health"];
		healthAttribute.OnValueChanged += HealthBar_OnValueChanged;
		Value = (float)healthAttribute.CurrentValue / healthAttribute.Max * 100;
		HealthBarLabel.Text = $"{healthAttribute.CurrentValue}/{healthAttribute.Max}";
	}

	private void HealthBar_OnValueChanged(EntityAttribute healthAttribute, int change)
	{
		Value = (float)healthAttribute.CurrentValue / healthAttribute.Max * 100;
		HealthBarLabel.Text = $"{healthAttribute.CurrentValue}/{healthAttribute.Max}";
	}
}
