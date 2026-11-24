// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A deathmatch player's number and score. Attributes reach every client, so every scoreboard reads the server's.
/// </summary>
public class ArenaAttributes : AttributeSet
{
	/// <summary>
	/// Gets the player's number, given by the server as it spawns the player. A dummy has none.
	/// </summary>
	public EntityAttribute Number { get; private set; }

	/// <summary>
	/// Gets how many players the player has fragged.
	/// </summary>
	public EntityAttribute Kills { get; private set; }

	/// <summary>
	/// Gets how many times the player has been fragged.
	/// </summary>
	public EntityAttribute Deaths { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ArenaAttributes"/> class.
	/// </summary>
	public ArenaAttributes()
	{
		Number = InitializeAttribute(nameof(Number), 0, 0, 99);
		Kills = InitializeAttribute(nameof(Kills), 0, 0, 9999);
		Deaths = InitializeAttribute(nameof(Deaths), 0, 0, 9999);
	}
}
