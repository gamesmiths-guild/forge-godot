// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Example;

public class CharacterAttributes : AttributeSet
{
	public Attribute Health { get; }

	public Attribute Mana { get; }

	public Attribute Strength { get; }

	public Attribute Agility { get; }

	public Attribute Intelligence { get; }

	public CharacterAttributes()
	{
		Health = InitializeAttribute(nameof(Health), 1000, 0, 1000);
		Mana = InitializeAttribute(nameof(Mana), 1000, 0, 1000);
		Strength = InitializeAttribute(nameof(Strength), 1, 0, 99);
		Agility = InitializeAttribute(nameof(Agility), 1, 0, 99);
		Intelligence = InitializeAttribute(nameof(Intelligence), 1, 0, 99);
	}
}
