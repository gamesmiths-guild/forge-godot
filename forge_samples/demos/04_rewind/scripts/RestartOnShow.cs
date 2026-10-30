// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Particles that start over each time they're shown. Hidden particles stop where they are instead of finishing, so a
/// fire put out in one place would otherwise flash there for a moment when the same body lights up somewhere else.
/// </summary>
public partial class RestartOnShow : GpuParticles3D
{
	public override void _Notification(int what)
	{
		base._Notification(what);

		if (what == NotificationVisibilityChanged && IsVisibleInTree())
		{
			Restart();
		}
	}
}
