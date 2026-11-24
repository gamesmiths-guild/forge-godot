// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The kill feed: a line for each frag's cue, which names the fragged entity as its target, the fragger as its source
/// and the fragger's weapon as its magnitude. The cue's source travels with it, so every machine names the same
/// fragger.
/// </summary>
public partial class KillFeedCueHandler : ForgeCueHandler
{
	/// <summary>
	/// Raised for each frag, with the fragged entity and the fragger.
	/// </summary>
	public event Action<IForgeEntity, IForgeEntity?>? Fragged;

	/// <summary>
	/// Gets or sets where the lines go.
	/// </summary>
	[Export]
	public required Container Feed { get; set; }

	/// <summary>
	/// Gets or sets the weapons' names, in the order the cue's magnitude counts them from 1.
	/// </summary>
	[Export]
	public string[] WeaponNames { get; set; } = [];

	/// <summary>
	/// Gets or sets how long a line stays, in seconds.
	/// </summary>
	[Export]
	public float LineSeconds { get; set; } = 6f;

	/// <summary>
	/// Gives the name to show for an entity: a player's number, a dummy's node.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns>Its name.</returns>
	public static string NameOf(IForgeEntity? entity)
	{
		if (entity is null)
		{
			return "Someone";
		}

		if (entity.Attributes.TryGetAttribute("ArenaAttributes.Number", out EntityAttribute? number)
			&& number.CurrentValue > 0)
		{
			return $"Player {number.CurrentValue}";
		}

		return entity is Node node && node.IsInsideTree() ? node.GetParent().Name : "Someone";
	}

	/// <inheritdoc/>
	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		IForgeEntity? fragger = parameters?.Source;
		int weapon = parameters?.Magnitude ?? 0;
		string with = weapon > 0 && weapon <= WeaponNames.Length ? $" [{WeaponNames[weapon - 1]}] " : " ";
		string line = fragger is null || fragger == forgeEntity
			? $"{NameOf(forgeEntity)} fragged themself"
			: NameOf(fragger) + with + NameOf(forgeEntity);

		var label = new Label { Text = line, HorizontalAlignment = HorizontalAlignment.Right };
		Feed.AddChild(label);

		Tween expiry = label.CreateTween();
		expiry.TweenInterval(LineSeconds);
		expiry.TweenCallback(Callable.From(label.QueueFree));

		Fragged?.Invoke(forgeEntity, fragger);
	}
}
