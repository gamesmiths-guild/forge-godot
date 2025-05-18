// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://cnjrjkgwyewjx")]
public partial class EffectArea2D : Area2D
{
	private EffectApplier? _effectApplier;

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

		switch (TriggerMode)
		{
			case EffectTriggerMode.OnEnter:
				BodyEntered += ApplyEffects;
				AreaEntered += ApplyEffects;
				break;

			case EffectTriggerMode.OnExit:
				BodyExited += ApplyEffects;
				AreaExited += ApplyEffects;
				break;

			case EffectTriggerMode.OnStay:
				BodyEntered += AddEffects;
				AreaEntered += AddEffects;
				BodyExited += RemoveEffects;
				AreaExited += RemoveEffects;
				break;
		}
	}

	private void ApplyEffects(Node2D node)
	{
		Debug.Assert(_effectApplier is not null, $"{_effectApplier} should have been initialized on _Ready().");
		_effectApplier.ApplyEffects(node, ForgeEntity);
	}

	private void AddEffects(Node2D node)
	{
		Debug.Assert(_effectApplier is not null, $"{_effectApplier} should have been initialized on _Ready().");
		_effectApplier.AddEffects(node, ForgeEntity);
	}

	private void RemoveEffects(Node2D node)
	{
		Debug.Assert(_effectApplier is not null, $"{_effectApplier} should have been initialized on _Ready().");
		_effectApplier.RemoveEffects(node);
	}
}
