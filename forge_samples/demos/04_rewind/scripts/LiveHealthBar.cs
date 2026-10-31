// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The Real-Time 3D demo's health bar, reading the attribute every frame instead of listening for changes, because
/// rolling the world back puts the attribute back without raising an event. It keeps updating while the game is paused,
/// so it follows the timeline as it's dragged.
/// </summary>
public partial class LiveHealthBar : ProgressBar
{
	private EntityAttribute? _health;

	[Export]
	public required Label HealthBarLabel { get; set; }

	[Export]
	public string Attribute { get; set; } = "Health";

	public override void _Ready()
	{
		base._Ready();

		ProcessMode = ProcessModeEnum.Always;
		ForgeEntity forgeEntity = GetParent().GetNode<ForgeEntity>("%Forge Entity");
		_health = forgeEntity.Attributes[$"CharacterAttributes.{Attribute}"];
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		Value = (float)_health!.CurrentValue / _health.Max * 100;
		HealthBarLabel.Text = $"{_health.CurrentValue}/{_health.Max}";
	}
}
