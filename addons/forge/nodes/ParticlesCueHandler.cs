// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Cue handler that drives a particle emitter already in the target's scene.
/// </summary>
/// <remarks>
/// <para>Emission follows the cue: applying starts it, removing stops it, and executing bursts. Pointing at an emitter
/// that is already in the scene rather than instantiating one is what keeps the effect's look where an artist authored
/// it - the material, the draw pass, the local-coords flag are all properties of that node.</para>
/// <para>One handler covers all four emitter types. They share no base class in Godot, so the operations switch over
/// them, which is also why <see cref="MagnitudeCurve"/> reaches only the GPU pair: <c>amount_ratio</c> is the one knob
/// that scales emission without reallocating the particle buffer, and the CPU emitters do not have it.</para>
/// </remarks>
[GlobalClass]
public partial class ParticlesCueHandler : ForgeCueHandler
{
	/// <summary>
	/// Gets or sets the path to the emitter, from the node the target lives on. Empty means the target's first emitter
	/// child.
	/// </summary>
	[Export]
	public string ParticlesPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether executing the cue restarts the emitter rather than only switching
	/// emission on. Restarting re-fires a burst from the beginning, which is what a repeated hit should look like;
	/// leaving it off lets a burst already in flight finish.
	/// </summary>
	[Export]
	public bool OneShotOnExecute { get; set; } = true;

	/// <summary>
	/// Gets or sets the curve scaling how much is emitted by the cue's normalized magnitude. Sampled at that magnitude
	/// and written as the emitter's amount ratio, so a weak hit and a heavy one differ in density. Unset means the
	/// authored amount.
	/// </summary>
	[Export]
	public Curve? MagnitudeCurve { get; set; }

	/// <inheritdoc/>
	public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (!TryGetEmitter(forgeEntity, out Node? emitter))
		{
			return;
		}

		ApplyMagnitude(emitter, parameters);
		SetEmitting(emitter, true);
	}

	/// <inheritdoc/>
	public override void _CueOnUpdate(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (TryGetEmitter(forgeEntity, out Node? emitter))
		{
			ApplyMagnitude(emitter, parameters);
		}
	}

	/// <inheritdoc/>
	public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
	{
		if (TryGetEmitter(forgeEntity, out Node? emitter))
		{
			SetEmitting(emitter, false);
		}
	}

	/// <inheritdoc/>
	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (!TryGetEmitter(forgeEntity, out Node? emitter))
		{
			return;
		}

		ApplyMagnitude(emitter, parameters);

		if (OneShotOnExecute)
		{
			Restart(emitter);
		}

		SetEmitting(emitter, true);
	}

	private static bool IsEmitter(Node node)
	{
		return node is GpuParticles3D or GpuParticles2D or CpuParticles3D or CpuParticles2D;
	}

	private static void SetEmitting(Node emitter, bool emitting)
	{
		switch (emitter)
		{
			case GpuParticles3D gpu3D:
				gpu3D.Emitting = emitting;
				break;

			case GpuParticles2D gpu2D:
				gpu2D.Emitting = emitting;
				break;

			case CpuParticles3D cpu3D:
				cpu3D.Emitting = emitting;
				break;

			case CpuParticles2D cpu2D:
				cpu2D.Emitting = emitting;
				break;
		}
	}

	private static void Restart(Node emitter)
	{
		switch (emitter)
		{
			case GpuParticles3D gpu3D:
				gpu3D.Restart();
				break;

			case GpuParticles2D gpu2D:
				gpu2D.Restart();
				break;

			case CpuParticles3D cpu3D:
				cpu3D.Restart();
				break;

			case CpuParticles2D cpu2D:
				cpu2D.Restart();
				break;
		}
	}

	private bool TryGetEmitter(IForgeEntity forgeEntity, [NotNullWhen(true)] out Node? emitter)
	{
		if (ForgeEntityBridge.TryGetEntityChild(forgeEntity, ParticlesPath, IsEmitter, out emitter))
		{
			return true;
		}

		WarnOnce(
			"found no particle emitter for its target" +
			(ParticlesPath.Length == 0 ? "." : $" at [{ParticlesPath}].") +
			" Nothing was emitted.");

		return false;
	}

	private void ApplyMagnitude(Node emitter, CueParameters? parameters)
	{
		if (MagnitudeCurve is null)
		{
			return;
		}

		float ratio = Mathf.Clamp(MagnitudeCurve.Sample(parameters?.NormalizedMagnitude ?? 0), 0.0f, 1.0f);

		switch (emitter)
		{
			case GpuParticles3D gpu3D:
				gpu3D.AmountRatio = ratio;
				break;

			case GpuParticles2D gpu2D:
				gpu2D.AmountRatio = ratio;
				break;

			default:
				WarnOnce(
					$"has a magnitude curve, which [{emitter.Name}] cannot follow: a CPU emitter has no amount " +
					"ratio, and changing its amount would reallocate its particles. Use a GPU emitter to scale by " +
					"magnitude.");
				break;
		}
	}
}
