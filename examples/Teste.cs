// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayCues.Godot;
using Gamesmiths.Forge.GameplayEffects.Components.Godot;
using Gamesmiths.Forge.GameplayEffects.Godot;
using Gamesmiths.Forge.GameplayEffects.Magnitudes.Godot;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class Teste : Node
{
	[Export]
	public ScalableFloat MyFloat { get; set; } = new ScalableFloat(0);

	[Export]
	public ScalableInt MyInteger { get; set; } = new ScalableInt(0);

	[Export]
	public TagContainer MyTagContainer { get; set; } = new();

	[Export]
	public GameplayCue MyGameplayCue { get; set; } = new();

	[Export]
	public EffectComponent MyEffectComponent { get; set; }

	[Export]
	public QueryExpression MyQueryExpression { get; set; }

	[Export]
	public Modifier MyModifier { get; set; }
}
