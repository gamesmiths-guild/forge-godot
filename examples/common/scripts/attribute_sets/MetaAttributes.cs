// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Example;

public class MetaAttributes : AttributeSet
{
	public EntityAttribute IncomingDamage { get; private set; }

	public MetaAttributes()
	{
		IncomingDamage = InitializeAttribute(nameof(IncomingDamage), 0, 0, 1000, 2);
	}
}
