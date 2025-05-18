// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Components;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
[Icon("uid://bcx7anhepqfmd")]
public abstract partial class EffectComponent : ForgeResource
{
	public abstract IGameplayEffectComponent GetComponent();
}
