// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class AbilityDelayTimer : Node
{
	private AbilityBehaviorContext? _context;
	private string _testProperty = string.Empty;
	private double _delaySeconds = 1.0;

	private Timer? _timer;
	private bool _initialized;

	public void Initialize(AbilityBehaviorContext context, string testProperty, double delaySeconds = 1.0)
	{
		_context = context;
		_testProperty = testProperty;
		_delaySeconds = delaySeconds;
		_initialized = true;

		if (IsInsideTree())
		{
			ArmTimer();
		}
	}

	public override void _Ready()
	{
		base._Ready();

		if (_initialized)
		{
			ArmTimer();
		}
	}

	public void CancelAndFree()
	{
		if (_timer?.IsStopped() == false)
		{
			_timer.Stop();
		}

		QueueFree();
	}

	private void ArmTimer()
	{
		if (_timer is not null)
		{
			return;
		}

		_timer = new Timer
		{
			OneShot = true,
			WaitTime = _delaySeconds,
			Autostart = true,
			ProcessCallback = Timer.TimerProcessCallback.Idle,
		};
		_timer.Timeout += OnTimeout;
		AddChild(_timer);
	}

	private void OnTimeout()
	{
		GD.Print($"Ability delayed action (after {_delaySeconds:0.##}s) with TestProperty: {_testProperty}");
		_context?.InstanceHandle.End();
		QueueFree();
	}
}
