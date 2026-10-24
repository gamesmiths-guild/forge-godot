// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;
using GdUnit4;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// Cues are dispatched at the end of the frame, so a cue handler can be handed a target that was freed during the
/// frame, such as an enemy that died from the hit the cue is for.
/// </summary>
[TestSuite]
public partial class ForgeCueHandlerTests
{
	[TestCase]
	[RequireGodotRuntime]
	public void A_freed_target_skips_the_targets_half_and_keeps_the_target_free_half()
	{
		var handler = new RecordingCueHandler();
		var entity = new ForgeEntity();
		entity.Free();

		try
		{
			handler.OnApply(entity, null);
			handler.OnExecute(entity, null);
			handler.OnUpdate(entity, null);
			handler.OnRemove(entity, false);

			handler.TargetCalls.Should().Be(0, "a freed node has nowhere to play a cue");
			handler.TargetFreeCalls.Should().Be(4);
		}
		finally
		{
			handler.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_live_target_gets_both_halves()
	{
		var handler = new RecordingCueHandler();
		var entity = new ForgeEntity();

		try
		{
			handler.OnExecute(entity, null);

			handler.TargetCalls.Should().Be(1);
			handler.TargetFreeCalls.Should().Be(1);
		}
		finally
		{
			entity.Free();
			handler.Free();
		}
	}

	private sealed partial class RecordingCueHandler : ForgeCueHandler
	{
		public int TargetCalls { get; private set; }

		public int TargetFreeCalls { get; private set; }

		public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
		{
			TargetCalls++;
		}

		public override void _CueOnApply(CueParameters? parameters)
		{
			TargetFreeCalls++;
		}

		public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
		{
			TargetCalls++;
		}

		public override void _CueOnExecute(CueParameters? parameters)
		{
			TargetFreeCalls++;
		}

		public override void _CueOnUpdate(IForgeEntity forgeEntity, CueParameters? parameters)
		{
			TargetCalls++;
		}

		public override void _CueOnUpdate(CueParameters? parameters)
		{
			TargetFreeCalls++;
		}

		public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
		{
			TargetCalls++;
		}

		public override void _CueOnRemove(bool interrupted)
		{
			TargetFreeCalls++;
		}
	}
}
