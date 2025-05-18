// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://d3wjawuu1ej3s")]
public partial class EffectRayCast2D : RayCast2D
{
	private EffectApplier? _effectApplier;

	private GodotObject? _lastFrameCollider;

	[Export]
	public Node? AreaOwner { get; set; }

	[Export]
	public EffectTriggerMode TriggerMode { get; set; }

	private IForgeEntity? ForgeEntity => AreaOwner as IForgeEntity;

	public override void _Ready()
	{
		if (AreaOwner is not null && AreaOwner is not IForgeEntity)
		{
			GD.PushError($"{nameof(AreaOwner)} must implement {nameof(IForgeEntity)}.");
		}

		base._Ready();

		_effectApplier = new EffectApplier(this);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		Debug.Assert(_effectApplier is not null, $"{_effectApplier} should have been initialized on _Ready().");

		GodotObject current = GetCollider();
		var hasCurrent = current is Node;
		var hadLast = _lastFrameCollider is Node;

		// Enter: is colliding now, wasn't colliding before.
		if (current is Node currentNode && !hadLast)
		{
			if (TriggerMode == EffectTriggerMode.OnStay)
			{
				_effectApplier.AddEffects(currentNode, ForgeEntity);
			}
			else if (TriggerMode == EffectTriggerMode.OnEnter)
			{
				_effectApplier.ApplyEffects(currentNode, ForgeEntity);
			}
		}

		// Exit: Was colliding before, isn't colliding now.
		if (!hasCurrent && _lastFrameCollider is Node lastNode)
		{
			if (TriggerMode == EffectTriggerMode.OnStay)
			{
				_effectApplier.RemoveEffects(lastNode);
			}
			else if (TriggerMode == EffectTriggerMode.OnExit)
			{
				_effectApplier.ApplyEffects(lastNode, ForgeEntity);
			}
		}

		_lastFrameCollider = hasCurrent ? current : null;
	}
}
