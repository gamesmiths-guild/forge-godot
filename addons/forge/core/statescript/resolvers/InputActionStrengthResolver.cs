// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves how far an input action is pressed, from 0 to 1.
/// </summary>
/// <remarks>
/// A digital button reads 0 or 1, so this is the analog form of Input Action Pressed: an analog trigger scaling a
/// shot's magnitude, a stick pushed part way scaling a movement speed.
/// </remarks>
/// <param name="actionName">The input action to read.</param>
internal sealed class InputActionStrengthResolver(string actionName) : InputResolverBase(actionName)
{
	private readonly StringName _actionName = actionName;

	public override Type ValueType => typeof(float);

	protected override Variant128 ResolveInput()
	{
		return new Variant128(Input.GetActionStrength(_actionName));
	}
}
