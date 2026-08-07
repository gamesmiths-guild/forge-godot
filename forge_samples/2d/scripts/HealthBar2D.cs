// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class HealthBar2D : ProgressBar
{
	[Export]
	public CustomForgeEntity? ForgeEntity { get; set; }

	public override void _Ready()
	{
		base._Ready();
		CallDeferred(nameof(Init));
	}

	public void Init()
	{
		if (ForgeEntity?.Attributes is null)
		{
			return;
		}

		EntityAttribute healthAttribute = ForgeEntity.Attributes["CharacterAttributes.Health"];
		healthAttribute.OnValueChanged += HealthBar_OnValueChanged;
		Value = (float)healthAttribute.CurrentValue / healthAttribute.Max * 100;
	}

	private void HealthBar_OnValueChanged(EntityAttribute healthAttribute, int change)
	{
		Value = (float)healthAttribute.CurrentValue / healthAttribute.Max * 100;
	}
}
