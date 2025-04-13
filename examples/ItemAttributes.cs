// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Example;

public class ItemAttributes : AttributeSet
{
	public Attribute Price { get; }

	public Attribute Weight { get; }

	public Attribute Rarity { get; }

	public ItemAttributes()
	{
		Price = InitializeAttribute(nameof(Price), 1000, 0, 1000);
		Weight = InitializeAttribute(nameof(Weight), 1000, 0, 1000);
		Rarity = InitializeAttribute(nameof(Rarity), 1, 0, 99);
	}
}
