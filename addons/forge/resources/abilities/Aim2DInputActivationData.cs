// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Providers;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

/// <summary>
/// Activates an ability bound to an input action with an <see cref="AimActivationData2D"/> built from the tick's
/// input: aimed from the entity's node, in the input's aim direction, at the input's aim point.
/// </summary>
/// <remarks>
/// An input without an aim - its collector has no aim source - aims along the node's facing instead, at the node
/// itself.
/// </remarks>
[Tool]
[GlobalClass]
public partial class Aim2DInputActivationData : ForgeInputActivationData
{
	/// <inheritdoc/>
	public override AbilityInputBinding CreateBinding(string action, InputEdge edge)
	{
		return AbilityInputBinding.ForAction(action, static (entity, input) => Build(entity, input), edge);
	}

	private static AimActivationData2D Build(IForgeEntity entity, EntityInput input)
	{
		Node2D? node = ForgeEntityBridge.TryGetOwningNode(entity, string.Empty, out Node? owner)
			? owner as Node2D
			: null;
		Vector2 origin = node?.GlobalPosition ?? Vector2.Zero;
		InputFrame frame = input.Current;

		if (frame.HasAim)
		{
			return new AimActivationData2D(
				origin,
				new Vector2(frame.AimDirection.X, frame.AimDirection.Y),
				new Vector2(frame.AimPoint.X, frame.AimPoint.Y));
		}

		Vector2 facing = node is null ? Vector2.Right : Vector2.Right.Rotated(node.GlobalRotation);

		return new AimActivationData2D(origin, facing, origin);
	}
}
