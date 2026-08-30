// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the direction four opposed input actions point in, as a vector no longer than 1.
/// </summary>
/// <remarks>
/// This is aiming without a camera: the stick or the movement keys are the aim, which is what a twin-stick shooter and
/// a keyboard-steered dash both want. Godot clamps the result to the unit circle, so a diagonal is not faster than a
/// straight line, and screen axes apply - up is −Y.
/// </remarks>
/// <param name="negativeXAction">The action pointing left.</param>
/// <param name="positiveXAction">The action pointing right.</param>
/// <param name="negativeYAction">The action pointing up.</param>
/// <param name="positiveYAction">The action pointing down.</param>
internal sealed class InputVector2Resolver(
	string negativeXAction,
	string positiveXAction,
	string negativeYAction,
	string positiveYAction)
	: InputResolverBase(negativeXAction, positiveXAction, negativeYAction, positiveYAction)
{
	private readonly StringName _negativeXAction = negativeXAction;
	private readonly StringName _positiveXAction = positiveXAction;
	private readonly StringName _negativeYAction = negativeYAction;
	private readonly StringName _positiveYAction = positiveYAction;

	public override Type ValueType => typeof(NumericsVector2);

	protected override Variant128 ResolveInput()
	{
		Vector2 vector = Input.GetVector(_negativeXAction, _positiveXAction, _negativeYAction, _positiveYAction);
		return new Variant128(new NumericsVector2(vector.X, vector.Y));
	}
}
