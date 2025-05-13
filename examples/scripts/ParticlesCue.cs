// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.Core.Godot;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayCues.Godot;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
public partial class ParticlesCue : Cue
{
	private readonly Dictionary<Node3D, Node3D?> _effectInstanceMapping = [];

	[Export]
	public PackedScene? FireEffectScene { get; set; }

	[Export]
	public bool UpdateEffectIntensity { get; set; }

	public override void _CueOnApply(ForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
		base._CueOnApply(forgeEntity, parameters);

		if (forgeEntity is null)
		{
			return;
		}

		if (forgeEntity.GetParent() is not Node3D parent)
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

	public override void _CueOnUpdate(ForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
		if (!UpdateEffectIntensity)
		{
			return;
		}

		base._CueOnUpdate(forgeEntity, parameters);

		if (forgeEntity.GetParent() is not Node3D parent
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

	public override void _CueOnRemove(ForgeEntity forgeEntity, bool interrupted)
	{
		base._CueOnRemove(forgeEntity, interrupted);

		GD.Print($"Remove effect {interrupted}");

		if (forgeEntity.GetParent() is not Node3D parent
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
