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
public partial class ParticlesCueHandler2D : ForgeCueHandler
{
	private readonly Dictionary<Node2D, Node2D?> _effectInstanceMapping = [];

	[Export]
	public PackedScene? PersistentEffectScene { get; set; }

	[Export]
	public PackedScene? InstantEffectScene { get; set; }

	[Export]
	public bool UpdateEffectIntensity { get; set; }

	[Export]
	public Vector2 Offset { get; set; } = new(0, 0);

	public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		base._CueOnApply(forgeEntity, parameters);

		if (PersistentEffectScene is null)
		{
			return;
		}

		if (forgeEntity is not Node2D node)
		{
			return;
		}

		Debug.Assert(PersistentEffectScene is not null, $"{nameof(PersistentEffectScene)} reference is missing.");

		Node2D effectInstance = PersistentEffectScene.Instantiate<Node2D>();

		if (!_effectInstanceMapping.TryAdd(node, effectInstance))
		{
			_effectInstanceMapping[node] = effectInstance;
		}

		node.AddChild(effectInstance);
		effectInstance.Translate(Offset);
	}

	public override void _CueOnUpdate(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (forgeEntity is not Node2D node)
		{
			return;
		}

		if (!UpdateEffectIntensity)
		{
			return;
		}

		base._CueOnUpdate(forgeEntity, parameters);

		if (!_effectInstanceMapping.TryGetValue(node, out Node2D? effectInstance)
			|| effectInstance is null)
		{
			return;
		}

		if (effectInstance is not GpuParticles2D particle2D)
		{
			return;
		}

		if (!parameters.HasValue)
		{
			return;
		}

		particle2D.Amount = parameters.Value.Magnitude;
	}

	public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
	{
		if (forgeEntity is not Node2D node)
		{
			return;
		}

		base._CueOnRemove(forgeEntity, interrupted);

		if (!_effectInstanceMapping.TryGetValue(node, out Node2D? effectInstance)
			|| effectInstance is null)
		{
			return;
		}

		node.RemoveChild(effectInstance);
		effectInstance.QueueFree();
		_effectInstanceMapping[node] = null;
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

		if (node.GetParent() is not Node2D parent)
		{
			return;
		}

		Node2D effectInstance = InstantEffectScene.Instantiate<Node2D>();

		parent.AddChild(effectInstance);
		effectInstance.Translate(Offset);

		if (effectInstance is not GpuParticles2D particles)
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
