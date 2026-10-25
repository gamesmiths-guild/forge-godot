// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// The core <see cref="InputActionResolver"/>, which also reads the local device when its input source entity has no
/// input of its own.
/// </summary>
/// <remarks>
/// This is the composable half of Input Action: a bool an Expression or a Condition Monitor can gate on, where the node
/// is what a graph waits on. Reading it inside a subgraph that already runs every frame is the usual reason to reach
/// for it. Read from an entity's input, "this frame" is the current physics tick.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="actionName">The input action to read.</param>
/// <param name="mode">Which question to ask about the button.</param>
internal sealed class InputActionPressedResolver(IEntityResolver inputSource, string actionName, InputActionMode mode)
	: InputActionResolver(inputSource, actionName, ToState(mode))
{
	private readonly StringName _actionName = actionName;
	private readonly InputActionMode _mode = mode;
	private readonly InputActionChecks _checks = new(nameof(InputActionPressedResolver), actionName);

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

		return new Variant128(_mode switch
		{
			InputActionMode.JustPressed => Input.IsActionJustPressed(_actionName),
			InputActionMode.JustReleased => Input.IsActionJustReleased(_actionName),
			_ => Input.IsActionPressed(_actionName),
		});
	}

	private static InputActionState ToState(InputActionMode mode)
	{
		return mode switch
		{
			InputActionMode.JustPressed => InputActionState.Pressed,
			InputActionMode.JustReleased => InputActionState.Released,
			_ => InputActionState.Held,
		};
	}
}
