// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Example;

public class ItemAttributes : AttributeSet
{
	public EntityAttribute Price { get; private set; }

	public EntityAttribute Weight { get; private set; }

	public EntityAttribute Rarity { get; private set; }

	public ItemAttributes()
	{
		Price = InitializeAttribute(nameof(Price), 1000, 0, 1000);
		Weight = InitializeAttribute(nameof(Weight), 1000, 0, 1000);
		Rarity = InitializeAttribute(nameof(Rarity), 1, 0, 99);
	}
}
