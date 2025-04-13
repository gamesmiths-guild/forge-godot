// Copyright © 2025 Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Core.Godot;

[Tool]
public partial class AttributeValues : Resource
{
	[Export]
	public int Current { get; set; }

	[Export]
	public int Min { get; set; }

	[Export]
	public int Max { get; set; }

	public AttributeValues()
	{
	}

	public AttributeValues(int current, int min, int max)
	{
		Current = current;
		Min = min;
		Max = max;
	}
}
