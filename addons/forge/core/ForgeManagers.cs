// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Godot.Core;

public class ForgeManagers
{
	private long? _nextTick;

	public static ForgeManagers Instance { get; private set; } = null!;

	public TagsManager TagsManager { get; private set; }

	public CuesManager CuesManager { get; private set; }

	/// <summary>
	/// Gets the session clock: the tick every entity simulates next, counted at the physics tick rate. Offline it
	/// starts at zero when the game starts; in a networked session it follows the server's.
	/// </summary>
	public SimulationClock Clock { get; }

	/// <summary>
	/// Gets or sets a value indicating whether this machine shows the game. A dedicated server doesn't, and skips
	/// the presentation passes: nothing they run may change gameplay, so the game simulates the same without them.
	/// </summary>
	public bool HasViewer { get; set; } = true;

	public ForgeManagers(string[] registeredTags, int tickRate = 60)
	{
		Instance = this;
		Clock = new SimulationClock(tickRate);

#if DEBUG
		Validation.Enabled = true;
#else
		Validation.Enabled = false;
#endif

		TagsManager = new TagsManager(registeredTags);
		CuesManager = new CuesManager();
	}

	/// <summary>
	/// Makes the next physics step simulate the given tick instead of the one after the current. A world history
	/// continuing from a tick it rolled back to uses it, and so does a client whose clock is steered toward the
	/// server's. Takes effect when the current step ends.
	/// </summary>
	/// <param name="tick">The tick the next physics step simulates.</param>
	public void SetNextTick(long tick)
	{
		_nextTick = tick;
	}

	// Called once at the end of every physics step, after every entity advanced: the clock moves to the requested
	// tick, or the next one.
	internal void EndTick()
	{
		if (_nextTick is long next)
		{
			_nextTick = null;
			Clock.Set(next);
			return;
		}

		Clock.Advance();
	}
}
