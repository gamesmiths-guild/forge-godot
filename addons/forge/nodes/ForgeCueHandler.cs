// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://snulmvxydrp4")]
public abstract partial class ForgeCueHandler : Node, ICueHandler
{
	[Export]
	public string? CueTag { get; set; }

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		base._Ready();

		if (string.IsNullOrEmpty(CueTag))
		{
			return;
		}

		ForgeManagers.Instance.CuesManager.RegisterCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, CueTag), this);
	}

#pragma warning disable CA1707, IDE1006, SA1300 // Identifiers should not contain underscores
	public void OnApply(IForgeEntity? target, CueParameters? parameters)
	{
		if (target is IForgeEntity forgeEntity)
		{
			_CueOnApply(forgeEntity, parameters);
		}

		_CueOnApply(parameters);
	}

	public virtual void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
	}

	public virtual void _CueOnApply(CueParameters? parameters)
	{
	}

	public void OnExecute(IForgeEntity? target, CueParameters? parameters)
	{
		if (target is IForgeEntity forgeEntity)
		{
			_CueOnExecute(forgeEntity, parameters);
		}

		_CueOnExecute(parameters);
	}

	public virtual void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
	}

	public virtual void _CueOnExecute(CueParameters? parameters)
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

	public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
	{
		if (target is IForgeEntity forgeEntity)
		{
			_CueOnUpdate(forgeEntity, parameters);
		}

		_CueOnUpdate(parameters);
	}

	public virtual void _CueOnUpdate(IForgeEntity forgeEntity, CueParameters? parameters)
	{
	}

	public virtual void _CueOnUpdate(CueParameters? parameters)
	{
	}
}
