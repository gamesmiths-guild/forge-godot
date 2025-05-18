// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Example;

public class ItemAttributes : AttributeSet
{
	public Attribute Price { get; private set; }

	public Attribute Weight { get; private set; }

	public Attribute Rarity { get; private set; }

	public ItemAttributes()
	{
		Price = InitializeAttribute(nameof(Price), 1000, 0, 1000);
		Weight = InitializeAttribute(nameof(Weight), 1000, 0, 1000);
		Rarity = InitializeAttribute(nameof(Rarity), 1, 0, 99);
	}
}
