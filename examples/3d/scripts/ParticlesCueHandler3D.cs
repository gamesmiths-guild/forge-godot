// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
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

		parent.AddChild(effectInstance);
		effectInstance.Translate(Offset);
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

		if (effectInstance is not GpuParticles3D particle3D)
		{
			return;
		}

		if (!parameters.HasValue)
		{
			return;
		}

		particle3D.Amount = parameters.Value.Magnitude;
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
		_effectInstanceMapping[parent] = null;
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

	private async Task DestroyAfter(Node node, float delay)
	{
		GD.Print($"Destroying node {node.Name} after {delay} seconds.");

		await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
		node.QueueFree();
	}
}
