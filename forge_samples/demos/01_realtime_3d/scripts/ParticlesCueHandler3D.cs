// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Threading.Tasks;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

[GlobalClass]
public partial class ParticlesCueHandler3D : ForgeCueHandler
{
	private readonly Dictionary<Node3D, Node3D?> _effectInstanceMapping = [];

	[Export]
	public PackedScene? PersistentEffectScene { get; set; }

	[Export]
	public PackedScene? InstantEffectScene { get; set; }

	[Export]
	public bool UpdateEffectIntensity { get; set; }

	// The emitter's speed at the lowest and the highest magnitude; the default leaves the scene at its authored pace.
	[Export]
	public Vector2 IntensitySpeed { get; set; } = new(1, 1);

	[Export]
	public Vector3 Offset { get; set; } = new(0, 0, 0);

	public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		base._CueOnApply(forgeEntity, parameters);

		if (PersistentEffectScene is null)
		{
			return;
		}

		if (forgeEntity is not Node node)
		{
			return;
		}

		if (node.GetParent() is not Node3D parent)
		{
			return;
		}

		Node3D effectInstance = PersistentEffectScene.Instantiate<Node3D>();

		if (!_effectInstanceMapping.TryAdd(parent, effectInstance))
		{
			_effectInstanceMapping[parent] = effectInstance;
		}

		// The entry goes when the effect leaves the tree, which covers a removal and a target freed mid-effect - an
		// enemy that dies burning - alike.
		effectInstance.TreeExiting += () =>
		{
			if (_effectInstanceMapping.TryGetValue(parent, out Node3D? tracked) && tracked == effectInstance)
			{
				_effectInstanceMapping.Remove(parent);
			}
		};

		parent.AddChild(effectInstance);
		effectInstance.Translate(Offset);
		ApplyIntensity(effectInstance, parameters);
	}

	public override void _CueOnUpdate(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (forgeEntity is not Node node)
		{
			return;
		}

		if (!UpdateEffectIntensity)
		{
			return;
		}

		base._CueOnUpdate(forgeEntity, parameters);

		if (node.GetParent() is not Node3D parent
			|| !_effectInstanceMapping.TryGetValue(parent, out Node3D? effectInstance)
			|| effectInstance is null)
		{
			return;
		}

		ApplyIntensity(effectInstance, parameters);
	}

	public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
	{
		if (forgeEntity is not Node node)
		{
			return;
		}

		base._CueOnRemove(forgeEntity, interrupted);

		if (node.GetParent() is not Node3D parent
			|| !_effectInstanceMapping.TryGetValue(parent, out Node3D? effectInstance)
			|| effectInstance is null)
		{
			return;
		}

		parent.RemoveChild(effectInstance);
		effectInstance.QueueFree();
	}

	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		base._CueOnExecute(forgeEntity, parameters);

		if (InstantEffectScene is null)
		{
			return;
		}

		if (forgeEntity is not Node node)
		{
			return;
		}

		if (node.GetParent() is not Node3D parent)
		{
			return;
		}

		Node3D effectInstance = InstantEffectScene.Instantiate<Node3D>();

		parent.AddChild(effectInstance);
		effectInstance.Translate(Offset);

		if (effectInstance is not GpuParticles3D particles)
		{
			return;
		}

		particles.Emitting = false;
		particles.Restart();
		particles.Emitting = true;

		_ = DestroyAfter(particles, (float)(particles.Lifetime + 0.1f));
	}

	// The amount ratio rather than the amount: the ratio scales emission without reallocating the particle buffer, and
	// it reads the cue's normalized magnitude, so an effect scene decides how dense "full" is.
	private void ApplyIntensity(Node3D effectInstance, CueParameters? parameters)
	{
		if (!UpdateEffectIntensity || effectInstance is not GpuParticles3D particles || !parameters.HasValue)
		{
			return;
		}

		float magnitude = parameters.Value.NormalizedMagnitude;

		particles.AmountRatio = magnitude;
		particles.SpeedScale = Mathf.Lerp(IntensitySpeed.X, IntensitySpeed.Y, magnitude);
	}

	private async Task DestroyAfter(Node node, float delay)
	{
		GD.Print($"Destroying node {node.Name} after {delay} seconds.");

		await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);

		// Gone already when its target was freed first.
		if (IsInstanceValid(node))
		{
			node.QueueFree();
		}
	}
}
