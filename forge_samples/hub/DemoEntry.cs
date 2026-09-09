// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

[GlobalClass]
public partial class DemoEntry : Resource
{
	[Export]
	public string Title { get; set; } = string.Empty;

	[Export]
	public string Tagline { get; set; } = string.Empty;

	[Export(PropertyHint.MultilineText)]
	public string Blurb { get; set; } = string.Empty;

	[Export]
	public Array<string> Highlights { get; set; } = [];

	[Export]
	public PackedScene? Scene { get; set; }

	[Export]
	public Color Accent { get; set; } = Colors.White;
}
