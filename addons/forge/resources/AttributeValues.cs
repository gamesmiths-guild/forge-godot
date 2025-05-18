// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
public partial class AttributeValues : ForgeResource
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
