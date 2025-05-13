// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.GameplayEffects.Components.Godot;

[Tool]
[GlobalClass]
public abstract partial class EffectComponent : Resource
{
	public abstract IGameplayEffectComponent GetComponent();
}
