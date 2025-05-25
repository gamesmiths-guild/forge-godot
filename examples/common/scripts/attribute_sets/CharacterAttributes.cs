// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Example;

public class CharacterAttributes : AttributeSet
{
	public EntityAttribute Health { get; private set; }

	public EntityAttribute Mana { get; private set; }

	public EntityAttribute Strength { get; private set; }

	public EntityAttribute Agility { get; private set; }

	public EntityAttribute Intelligence { get; private set; }

	public CharacterAttributes()
	{
		Health = InitializeAttribute(nameof(Health), 1000, 0, 1000);
		Mana = InitializeAttribute(nameof(Mana), 1000, 0, 1000);
		Strength = InitializeAttribute(nameof(Strength), 1, 0, 99);
		Agility = InitializeAttribute(nameof(Agility), 1, 0, 99);
		Intelligence = InitializeAttribute(nameof(Intelligence), 1, 0, 99);
	}
}
