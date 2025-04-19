// Copyright © 2025 Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Core.Godot;

[Tool]
public partial class AttributeValues : Resource
{
	[Export]
	public int Default { get; set; }

	[Export]
	public int Min { get; set; }

	[Export]
	public int Max { get; set; }

	public AttributeValues()
	{
	}

	public AttributeValues(int @default, int min, int max)
	{
		Default = @default;
		Min = min;
		Max = max;
	}
}
