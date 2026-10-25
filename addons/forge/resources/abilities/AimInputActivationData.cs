// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Providers;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

/// <summary>
/// Activates an ability bound to an input action with an <see cref="AimActivationData"/> built from the tick's input:
/// aimed from the entity's node at the input's aim point.
/// </summary>
/// <remarks>
/// An input without an aim - its collector has no aim source - aims along the node's horizontal forward instead, at the
/// node itself.
/// </remarks>
[Tool]
[GlobalClass]
public partial class AimInputActivationData : ForgeInputActivationData
{
	/// <inheritdoc/>
	public override AbilityInputBinding CreateBinding(string action, InputEdge edge)
	{
		return AbilityInputBinding.ForAction(action, static (entity, input) => Build(entity, input), edge);
	}

	private static AimActivationData Build(IForgeEntity entity, EntityInput input)
	{
		Node3D? node = ForgeEntityBridge.TryGetOwningNode(entity, string.Empty, out Node? owner)
			? owner as Node3D
			: null;
		Vector3 origin = node?.GlobalPosition ?? Vector3.Zero;
		InputFrame frame = input.Current;

		if (frame.HasAim)
		{
			var direction = new Vector3(frame.AimDirection.X, frame.AimDirection.Y, frame.AimDirection.Z);
			var point = new Vector3(frame.AimPoint.X, frame.AimPoint.Y, frame.AimPoint.Z);

			// An aim taken from elsewhere, such as a camera behind the entity, is turned to meet the same point from
			// the entity. One that already points at it across the ground from the entity, as the ground aim does,
			// stays horizontal; loosely, since the input carries the direction quantized.
			Vector3 toPoint = point - origin;
			Vector3 acrossGround = new Vector3(toPoint.X, 0, toPoint.Z).Normalized();

			if (toPoint.IsZeroApprox() || direction.Dot(acrossGround) > 0.9999f)
			{
				return new AimActivationData(origin, direction, point);
			}

			return new AimActivationData(origin, toPoint.Normalized(), point);
		}

		Vector3 forward = node is null ? Vector3.Forward : -node.GlobalBasis.Z;
		forward.Y = 0;

		return new AimActivationData(
			origin,
			forward.LengthSquared() > 0 ? forward.Normalized() : Vector3.Forward,
			origin);
	}
}
