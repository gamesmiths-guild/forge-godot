// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core.Statescript.Providers;
using Gamesmiths.Forge.Godot.Resources.Abilities;

namespace Gamesmiths.Forge.Godot.Tests.Helpers;

/// <summary>
/// An ability behavior resource whose behavior records the aim it was activated with and ends at once.
/// </summary>
internal sealed partial class RecordingAimBehavior : ForgeAbilityBehavior
{
	public AimActivationData? Received { get; private set; }

	public int Activations { get; private set; }

	public override IAbilityBehavior GetBehavior()
	{
		return new AimBehavior(this);
	}

	private sealed class AimBehavior(RecordingAimBehavior owner) : IAbilityBehavior<AimActivationData>
	{
		private readonly RecordingAimBehavior _owner = owner;

		public void OnStarted(AbilityBehaviorContext context, AimActivationData data)
		{
			_owner.Received = data;
			_owner.Activations++;
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}
}
