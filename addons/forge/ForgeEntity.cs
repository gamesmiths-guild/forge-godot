// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot;

[Tool]
public partial class ForgeEntity : Node, IForgeEntity
{
	public Attributes Attributes { get; set; }

	public Core.GameplayTags GameplayTags { get; set; }

	public GameplayEffectsManager EffectsManager { get; set; }

	[Export]
	public Array<string> ContainerTags { get; set; } = [];

	public override void _Ready()
	{
		base._Ready();

		var tags = new HashSet<GameplayTag>();

		foreach (var tag in ContainerTags)
		{
			try
			{
				tags.Add(GameplayTag.RequestTag(Forge.TagsManager, tag));
			}
			catch (GameplayTagNotRegisteredException)
			{
				GD.PushWarning($"Tag [{tag}] is not registered.");
			}
		}

		EffectsManager = new GameplayEffectsManager(this, Forge.CuesManager);

		GameplayTags = new(new GameplayTagContainer(Forge.TagsManager, tags));
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		base._Process(delta);

		EffectsManager.UpdateEffects(delta);
	}
}
