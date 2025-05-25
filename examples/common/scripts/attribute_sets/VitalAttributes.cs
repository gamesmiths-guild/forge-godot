// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Godot;

namespace Gamesmiths.Forge.Example;

public class VitalAttributes : AttributeSet
{
	public EntityAttribute Vitality { get; private set; }

	public EntityAttribute MaxHealth { get; private set; }

	public EntityAttribute CurrentHealth { get; private set; }

	public VitalAttributes()
	{
		Vitality = InitializeAttribute(nameof(Vitality), 10, 0, 99);
		MaxHealth = InitializeAttribute(nameof(MaxHealth), Vitality.CurrentValue * 10, 0, 1000);
		CurrentHealth = InitializeAttribute(nameof(CurrentHealth), 100, 0, MaxHealth.CurrentValue);
	}

	protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
	{
		base.AttributeOnValueChanged(attribute, change);

		if (attribute == Vitality)
		{
			// Do health to vit calculations here.
			SetAttributeMaxValue(MaxHealth, Vitality.CurrentValue * 10);
		}

		if (attribute == MaxHealth)
		{
			SetAttributeMaxValue(CurrentHealth, MaxHealth.CurrentValue);
		}

		if (attribute == CurrentHealth)
		{
			if (change < 0)
			{
				GD.Print($"Damage: {change}");

				if (CurrentHealth.CurrentValue <= 0)
				{
					GD.Print("Death");
				}
			}
			else
			{
				GD.Print($"Healing: {change}");
			}
		}
	}
}
