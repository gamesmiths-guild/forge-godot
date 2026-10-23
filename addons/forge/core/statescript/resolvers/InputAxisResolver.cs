// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeInputAxisResolver = Gamesmiths.Forge.Statescript.Properties.InputAxisResolver;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// The core <see cref="ForgeInputAxisResolver"/>, which also reads the local device when its input source entity has
/// no input of its own.
/// </summary>
/// <remarks>
/// The two actions cancel, so holding both reads zero rather than whichever was pressed last. That is the difference
/// from subtracting two Input Action Strength resolvers, which is also why the pair earns a resolver of its own.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="negativeAction">The action driving the value towards −1.</param>
/// <param name="positiveAction">The action driving the value towards 1.</param>
internal sealed class InputAxisResolver(IEntityResolver inputSource, string negativeAction, string positiveAction)
	: ForgeInputAxisResolver(inputSource, negativeAction, positiveAction)
{
	private readonly StringName _negativeAction = negativeAction;
	private readonly StringName _positiveAction = positiveAction;
	private readonly InputActionChecks _checks = new(nameof(InputAxisResolver), negativeAction, positiveAction);

	protected override Variant128 ResolveInput(EntityInput input)
	{
		_checks.ReportUncarried(input);
		return base.ResolveInput(input);
	}

	protected override Variant128 ResolveWithoutInput(GraphContext graphContext)
	{
		return _checks.DeviceHasActions()
			? new Variant128(Input.GetAxis(_negativeAction, _positiveAction))
			: default;
	}
}
