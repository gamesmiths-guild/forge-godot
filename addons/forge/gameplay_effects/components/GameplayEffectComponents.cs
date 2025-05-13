// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Components.Godot;

/// <summary>
/// TODO: Change this to dynamically fetch components. The user could implement their own mechanics and by using this
/// enum they won't be listed on the inspector unless they add them here.
/// </summary>
public enum GameplayEffectComponents
{
	/// <summary>
	/// Use ChanceToApply component.
	/// </summary>
	ChanceToApply = 0,

	/// <summary>
	/// Use TargetTagRequirements component.
	/// </summary>
	TargetTagRequirements = 1,

	/// <summary>
	/// Use ModifierTags component.
	/// </summary>
	ModifierTags = 2,
}
