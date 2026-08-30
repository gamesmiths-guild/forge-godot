// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the signed strength of a pair of opposed input actions, from −1 to 1.
/// </summary>
/// <remarks>
/// The two actions cancel, so holding both reads zero rather than whichever was pressed last. That is the difference
/// from subtracting two Input Action Strength resolvers, which is also why the pair earns a resolver of its own.
/// </remarks>
/// <param name="negativeAction">The action driving the value towards −1.</param>
/// <param name="positiveAction">The action driving the value towards 1.</param>
internal sealed class InputAxisResolver(string negativeAction, string positiveAction)
	: InputResolverBase(negativeAction, positiveAction)
{
	private readonly StringName _negativeAction = negativeAction;
	private readonly StringName _positiveAction = positiveAction;

	public override Type ValueType => typeof(float);

	protected override Variant128 ResolveInput()
	{
		return new Variant128(Input.GetAxis(_negativeAction, _positiveAction));
	}
}
