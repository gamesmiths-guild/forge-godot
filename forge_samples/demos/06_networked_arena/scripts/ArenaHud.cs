// Copyright © Gamesmiths Guild.

using System.Linq;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// This machine's player's HUD: health, the weapon held, who fragged it while it waits to respawn, and the scoreboard
/// while Tab is held. Everything it shows is the player's entity, and the others', as this machine holds them.
/// </summary>
public partial class ArenaHud : CanvasLayer
{
	private static readonly Color _held = new(1, 0.85f, 0.3f);
	private static readonly Color _stowed = new(1, 1, 1, 0.45f);

	private string _fraggedBy = string.Empty;

	/// <summary>
	/// Gets or sets the session.
	/// </summary>
	[Export]
	public required ForgeNetworkSession Session { get; set; }

	/// <summary>
	/// Gets or sets the kill feed, which says who fragged this machine's player.
	/// </summary>
	[Export]
	public required KillFeedCueHandler KillFeed { get; set; }

	/// <summary>
	/// Gets or sets the health readout.
	/// </summary>
	[Export]
	public required Label Health { get; set; }

	/// <summary>
	/// Gets or sets the weapon slots, in the order of <see cref="WeaponTags"/>.
	/// </summary>
	[Export]
	public Array<Label> WeaponSlots { get; set; } = [];

	/// <summary>
	/// Gets or sets the tag that says each weapon is held.
	/// </summary>
	[Export]
	public Array<ForgeTag> WeaponTags { get; set; } = [];

	/// <summary>
	/// Gets or sets the tag a fragged entity has until it respawns.
	/// </summary>
	[Export]
	public ForgeTag? DeadTag { get; set; }

	/// <summary>
	/// Gets or sets what shows while this machine's player waits to respawn.
	/// </summary>
	[Export]
	public required Control DeadOverlay { get; set; }

	/// <summary>
	/// Gets or sets the label on the dead overlay.
	/// </summary>
	[Export]
	public required Label DeadLabel { get; set; }

	/// <summary>
	/// Gets or sets the scoreboard, shown while the scoreboard action is held.
	/// </summary>
	[Export]
	public required Control Scoreboard { get; set; }

	/// <summary>
	/// Gets or sets the scoreboard's rows: a grid of three columns, the first row its header.
	/// </summary>
	[Export]
	public required GridContainer ScoreRows { get; set; }

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		Visible = false;
		KillFeed.Fragged += OnFragged;
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		ForgeEntity? player = Session.LocalPlayer;
		Visible = player is not null;

		if (player is null)
		{
			return;
		}

		Health.Text = $"{Read(player, "CharacterAttributes.Health")}";

		for (int i = 0; i < WeaponSlots.Count && i < WeaponTags.Count; i++)
		{
			WeaponSlots[i].Modulate = player.Tags.AllTags.HasTag(WeaponTags[i].GetTag()) ? _held : _stowed;
		}

		DeadOverlay.Visible = DeadTag is not null && player.Tags.AllTags.HasTag(DeadTag.GetTag());
		DeadLabel.Text = $"Fragged by {_fraggedBy}.\nBack in a moment.";

		Scoreboard.Visible = Input.IsActionPressed("scoreboard");

		if (Scoreboard.Visible)
		{
			FillScoreboard(player);
		}
	}

	private static int Read(IForgeEntity entity, string attribute)
	{
		return entity.Attributes.TryGetAttribute(attribute, out EntityAttribute? value) ? value.CurrentValue : 0;
	}

	private void OnFragged(IForgeEntity fragged, IForgeEntity? fragger)
	{
		if (fragged == Session.LocalPlayer)
		{
			_fraggedBy = fragger is null || fragger == fragged ? "yourself" : KillFeedCueHandler.NameOf(fragger);
		}
	}

	private void FillScoreboard(ForgeEntity player)
	{
		// The header's three labels stay.
		foreach (Node row in ScoreRows.GetChildren().Skip(3))
		{
			ScoreRows.RemoveChild(row);
			row.QueueFree();
		}

		IForgeEntity[] players = [.. GetTree().GetNodesInGroup(ArenaGame.PlayersGroup)
			.Select(x => x.GetNodeOrNull<ForgeEntity>("Forge Entity"))
			.OfType<IForgeEntity>()
			.OrderByDescending(x => Read(x, "ArenaAttributes.Kills"))
			.ThenBy(x => Read(x, "ArenaAttributes.Deaths"))];

		foreach (IForgeEntity entry in players)
		{
			string name = KillFeedCueHandler.NameOf(entry) + (entry == player ? " (you)" : string.Empty);

			foreach (string text in new[]
			{
				name,
				$"{Read(entry, "ArenaAttributes.Kills")}",
				$"{Read(entry, "ArenaAttributes.Deaths")}",
			})
			{
				ScoreRows.AddChild(new Label { Text = text });
			}
		}
	}
}
