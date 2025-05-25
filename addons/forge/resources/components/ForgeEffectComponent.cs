// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
[Icon("uid://bcx7anhepqfmd")]
public abstract partial class ForgeEffectComponent : Resource
{
	public abstract IEffectComponent GetComponent();
}
