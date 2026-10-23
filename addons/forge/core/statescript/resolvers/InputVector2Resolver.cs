// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// The core <see cref="InputVectorResolver"/>, which also reads the local device when its input source entity has no
/// input of its own.
/// </summary>
/// <remarks>
/// This is aiming without a camera: the stick or the movement keys are the aim, which is what a twin-stick shooter and
/// a keyboard-steered dash both want. The result is clamped to the unit circle, so a diagonal is not faster than a
/// straight line, and screen axes apply - up is −Y. Read from an entity's input, the four actions give the input's
/// vector when it carries one for them, and are read as buttons otherwise.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="negativeXAction">The action pointing left.</param>
/// <param name="positiveXAction">The action pointing right.</param>
/// <param name="negativeYAction">The action pointing up.</param>
/// <param name="positiveYAction">The action pointing down.</param>
internal sealed class InputVector2Resolver(
	IEntityResolver inputSource,
	string negativeXAction,
	string positiveXAction,
	string negativeYAction,
	string positiveYAction)
	: InputVectorResolver(inputSource, negativeXAction, positiveXAction, negativeYAction, positiveYAction)
{
	private readonly StringName _negativeXAction = negativeXAction;
	private readonly StringName _positiveXAction = positiveXAction;
	private readonly StringName _negativeYAction = negativeYAction;
	private readonly StringName _positiveYAction = positiveYAction;

	private readonly InputActionChecks _checks = new(
		nameof(InputVector2Resolver),
		negativeXAction,
		positiveXAction,
		negativeYAction,
		positiveYAction);

	protected override Variant128 ResolveInput(EntityInput input)
	{
		_checks.ReportUncarried(input);
		return base.ResolveInput(input);
	}

	protected override Variant128 ResolveWithoutInput(GraphContext graphContext)
	{
		if (!_checks.DeviceHasActions())
		{
			return default;
		}

		Vector2 vector = Input.GetVector(_negativeXAction, _positiveXAction, _negativeYAction, _positiveYAction);
		return new Variant128(new NumericsVector2(vector.X, vector.Y));
	}
}
