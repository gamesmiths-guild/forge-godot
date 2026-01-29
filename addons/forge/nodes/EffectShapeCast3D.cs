// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://e2ynjdppura3")]
public partial class EffectShapeCast3D : ShapeCast3D
{
	private readonly HashSet<GodotObject> _lastFrameColliders = [];

	private EffectApplier? _effectApplier;

	[Export]
	public Node? EffectOwner { get; set; }

	[Export]
	public Node? EffectSource { get; set; }

	[Export]
	public int EffectLevel { get; set; } = 1;

	[Export]
	public EffectTriggerMode TriggerMode { get; set; }

	private IForgeEntity? OwnerEntity => EffectOwner as IForgeEntity;

	private IForgeEntity? SourceEntity => EffectSource as IForgeEntity;

	public override void _Ready()
	{
		if (EffectOwner is not null && EffectOwner is not IForgeEntity)
		{
			GD.PushError($"{nameof(EffectOwner)} must implement {nameof(IForgeEntity)}.");
		}

		base._Ready();

		_effectApplier = new EffectApplier(this);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		Debug.Assert(_effectApplier is not null, $"{_effectApplier} should have been initialized on _Ready().");

		var collisions = GetCollisionCount();

		var collidersThisFrame = new List<GodotObject>();
		for (var i = 0; i < collisions; i++)
		{
			GodotObject current = GetCollider(i);
			var hadLast = _lastFrameColliders.Contains(current);

			_lastFrameColliders.Add(current);
			collidersThisFrame.Add(current);

			// Enter: is colliding now, wasn't colliding before.
			if (current is Node currentNode && !hadLast)
			{
				if (TriggerMode == EffectTriggerMode.OnStay)
				{
					_effectApplier.AddEffects(currentNode, OwnerEntity, SourceEntity, EffectLevel);
				}
				else if (TriggerMode == EffectTriggerMode.OnEnter)
				{
					_effectApplier.ApplyEffects(currentNode, OwnerEntity, SourceEntity, EffectLevel);
				}
			}
		}

		// Exit: Was colliding before, isn't colliding now.
		foreach (GodotObject? lastCollider in _lastFrameColliders.Except(collidersThisFrame))
		{
			_lastFrameColliders.Remove(lastCollider);

			if (lastCollider is not Node lastNode)
			{
				continue;
			}

			if (TriggerMode == EffectTriggerMode.OnStay)
			{
				_effectApplier.RemoveEffects(lastNode);
			}
			else if (TriggerMode == EffectTriggerMode.OnExit)
			{
				_effectApplier.ApplyEffects(lastNode, OwnerEntity, SourceEntity, EffectLevel);
			}
		}
	}
}
