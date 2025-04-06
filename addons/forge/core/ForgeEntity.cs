// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;
using Godot;
using Godot.Collections;

using static Gamesmiths.Forge.Godot.Forge;

namespace Gamesmiths.Forge.Core.Godot;

[Tool]
public partial class ForgeEntity : Node, IForgeEntity
{
	public Attributes Attributes { get; set; }

	public GameplayTags GameplayTags { get; set; }

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
				tags.Add(GameplayTag.RequestTag(TagsManager, tag));
			}
			catch (GameplayTagNotRegisteredException)
			{
				GD.PushWarning($"Tag [{tag}] is not registered.");
			}
		}

		EffectsManager = new GameplayEffectsManager(this, CuesManager);

		GameplayTags = new(new GameplayTagContainer(TagsManager, tags));
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
