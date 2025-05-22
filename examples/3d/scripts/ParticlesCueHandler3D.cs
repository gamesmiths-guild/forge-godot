// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

[GlobalClass]
public partial class ParticlesCueHandler3D : CueHandler
{
	private readonly Dictionary<Node3D, Node3D?> _effectInstanceMapping = [];

	[Export]
	public PackedScene? FireEffectScene { get; set; }

	[Export]
	public bool UpdateEffectIntensity { get; set; }

	public override void _CueOnApply(IForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
		base._CueOnApply(forgeEntity, parameters);

		if (forgeEntity is not Node node)
		{
			return;
		}

		if (node.GetParent() is not Node3D parent)
		{
			return;
		}

		Debug.Assert(FireEffectScene is not null, $"{nameof(FireEffectScene)} reference is missing.");

		Node3D effectInstance = FireEffectScene.Instantiate<Node3D>();

		if (!_effectInstanceMapping.TryAdd(parent, effectInstance))
		{
			_effectInstanceMapping[parent] = effectInstance;
		}

		parent.AddChild(effectInstance);
		effectInstance.Translate(new Vector3(0, 2, 0));
	}

	public override void _CueOnUpdate(IForgeEntity forgeEntity, GameplayCueParameters? parameters)
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
}
