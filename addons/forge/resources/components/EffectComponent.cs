// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
[Icon("uid://bcx7anhepqfmd")]
public abstract partial class EffectComponent : Resource
{
	public abstract IGameplayEffectComponent GetComponent();
}
