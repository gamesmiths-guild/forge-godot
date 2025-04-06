// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

using static Gamesmiths.Forge.Godot.Forge;

namespace Gamesmiths.Forge.Core.Godot;

[Tool]
public partial class ForgeEntity : Node, IForgeEntity
{
	[Export]
	public TagContainer BaseTags { get; set; } = new();

	public Attributes Attributes { get; set; }

	public GameplayTags Tags { get; set; }

	public GameplayEffectsManager EffectsManager { get; set; }

	GameplayTags IForgeEntity.GameplayTags => throw new System.NotImplementedException();

	public override void _Ready()
	{
		base._Ready();

		Tags = new(BaseTags.GetTagContainer());
		EffectsManager = new GameplayEffectsManager(this, CuesManager);
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
