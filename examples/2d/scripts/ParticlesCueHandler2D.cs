// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
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
	public PackedScene? FireEffectScene { get; set; }

	[Export]
	public bool UpdateEffectIntensity { get; set; }

	public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		base._CueOnApply(forgeEntity, parameters);

		if (forgeEntity is not Node2D node)
		{
			return;
		}

		Debug.Assert(FireEffectScene is not null, $"{nameof(FireEffectScene)} reference is missing.");

		Node2D effectInstance = FireEffectScene.Instantiate<Node2D>();

		if (!_effectInstanceMapping.TryAdd(node, effectInstance))
		{
			_effectInstanceMapping[node] = effectInstance;
		}

		node.AddChild(effectInstance);
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
}
