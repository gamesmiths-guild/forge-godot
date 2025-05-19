// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Godot;
using Godot.Collections;

using static Gamesmiths.Forge.Godot.Core.Forge;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://snulmvxydrp4")]
public abstract partial class CueHandler : Node, IGameplayCue
{
	[Export]
	public string? CueKey { get; set; }

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		base._Ready();

		if (CuesManager is null || string.IsNullOrEmpty(CueKey))
		{
			return;
		}

		CuesManager.RegisterCue(CueKey, this);
	}

	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.CueKey)
		{
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = string.Join(",", GetCueOptions());
		}
	}

#pragma warning disable CA1707, IDE1006, SA1300 // Identifiers should not contain underscores
	public void OnApply(IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (target is IForgeEntity forgeEntity)
		{
			_CueOnApply(forgeEntity, parameters);
		}

		_CueOnApply(parameters);
	}

	public virtual void _CueOnApply(IForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
	}

	public virtual void _CueOnApply(GameplayCueParameters? parameters)
	{
	}

	public void OnExecute(IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (target is IForgeEntity forgeEntity)
		{
			_CueOnExecute(forgeEntity, parameters);
		}

		_CueOnExecute(parameters);
	}

	public virtual void _CueOnExecute(IForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
	}

	public virtual void _CueOnExecute(GameplayCueParameters? parameters)
	{
	}

	public void OnRemove(IForgeEntity? target, bool interrupted)
	{
		if (target is IForgeEntity forgeEntity)
		{
			_CueOnRemove(forgeEntity, interrupted);
		}

		_CueOnRemove(interrupted);
	}

	public virtual void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
	{
	}

	public virtual void _CueOnRemove(bool interrupted)
	{
	}

	public void OnUpdate(IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (target is IForgeEntity forgeEntity)
		{
			_CueOnUpdate(forgeEntity, parameters);
		}

		_CueOnUpdate(parameters);
	}

	public virtual void _CueOnUpdate(IForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
	}

	public virtual void _CueOnUpdate(GameplayCueParameters? parameters)
	{
	}

	private static string[] GetCueOptions()
	{
		if (RegisteredCues is null)
		{
			return [];
		}

		return [.. RegisteredCues];
	}
}
