// Copyright © 2025 Gamesmiths Guild.

using System.Linq;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Core.Godot;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;
using Godot.Collections;
using static Gamesmiths.Forge.Godot.Forge;

namespace Gamesmiths.Forge.GameplayCues.Godot;

[Tool]
public abstract partial class Cue : Node, IGameplayCue
{
	[Export]
	public required string CueKey { get; set; }

	public override void _Ready()
	{
		base._Ready();

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

	public void OnApply(IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (target is ForgeEntity forgeEntity)
		{
			_CueOnApply(forgeEntity, parameters);
		}

		_CueOnApply(parameters);
	}

#pragma warning disable CA1707, IDE1006, SA1300 // Identifiers should not contain underscores
	public virtual void _CueOnApply(ForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
	}

	public virtual void _CueOnApply(GameplayCueParameters? parameters)
	{
	}

	public void OnExecute(IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (target is ForgeEntity forgeEntity)
		{
			_CueOnExecute(forgeEntity, parameters);
		}

		_CueOnExecute(parameters);
	}

	public virtual void _CueOnExecute(ForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
	}

	public virtual void _CueOnExecute(GameplayCueParameters? parameters)
	{
	}

	public void OnRemove(IForgeEntity? target, bool interrupted)
	{
		if (target is ForgeEntity forgeEntity)
		{
			_CueOnRemove(forgeEntity, interrupted);
		}

		_CueOnRemove(interrupted);
	}

	public virtual void _CueOnRemove(ForgeEntity forgeEntity, bool interrupted)
	{
	}

	public virtual void _CueOnRemove(bool interrupted)
	{
	}

	public void OnUpdate(IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (target is ForgeEntity forgeEntity)
		{
			_CueOnUpdate(forgeEntity, parameters);
		}

		_CueOnUpdate(parameters);
	}

	public virtual void _CueOnUpdate(ForgeEntity forgeEntity, GameplayCueParameters? parameters)
	{
	}

	public virtual void _CueOnUpdate(GameplayCueParameters? parameters)
	{
	}

	private static string[] GetCueOptions()
	{
		ForgePluginData pluginData =
			ResourceLoader.Load<ForgePluginData>("res://addons/forge/forge_data.tres");

		return pluginData.RegisteredCues.ToArray();
	}
}
