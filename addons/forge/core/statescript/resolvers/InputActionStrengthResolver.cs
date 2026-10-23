// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// The core <see cref="InputStrengthResolver"/>, which also reads the local device when its input source entity has no
/// input of its own.
/// </summary>
/// <remarks>
/// A digital button reads 0 or 1, so this is the analog form of Input Action Pressed: an analog trigger scaling a
/// shot's magnitude, a stick pushed part way scaling a movement speed. Read from an entity's input, only an action that
/// is a direction of one of the input's vectors has analog values.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="actionName">The input action to read.</param>
internal sealed class InputActionStrengthResolver(IEntityResolver inputSource, string actionName)
	: InputStrengthResolver(inputSource, actionName)
{
	private readonly StringName _actionName = actionName;
	private readonly InputActionChecks _checks = new(nameof(InputActionStrengthResolver), actionName);

	protected override Variant128 ResolveInput(EntityInput input)
	{
		_checks.ReportUncarried(input);
		return base.ResolveInput(input);
	}

	protected override Variant128 ResolveWithoutInput(GraphContext graphContext)
	{
		return _checks.DeviceHasActions() ? new Variant128(Input.GetActionStrength(_actionName)) : default;
	}
}
