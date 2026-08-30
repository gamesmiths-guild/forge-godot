// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves whether an input action is down, went down this frame, or came up this frame.
/// </summary>
/// <remarks>
/// This is the composable half of Input Action: a bool an Expression or a Condition Monitor can gate on, where the node
/// is what a graph waits on. Reading it inside a subgraph that already runs every frame is the usual reason to reach
/// for it.
/// </remarks>
/// <param name="actionName">The input action to read.</param>
/// <param name="mode">Which question to ask about the button.</param>
internal sealed class InputActionPressedResolver(string actionName, InputActionMode mode)
	: InputResolverBase(actionName)
{
	private readonly StringName _actionName = actionName;
	private readonly InputActionMode _mode = mode;

	public override Type ValueType => typeof(bool);

	protected override Variant128 ResolveInput()
	{
		return new Variant128(_mode switch
		{
			InputActionMode.JustPressed => Input.IsActionJustPressed(_actionName),
			InputActionMode.JustReleased => Input.IsActionJustReleased(_actionName),
			_ => Input.IsActionPressed(_actionName),
		});
	}
}
