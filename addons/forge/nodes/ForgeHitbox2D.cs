// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// A part of a 2D entity a shot can hit, as a head or a torso: a body of collision shapes that moves with the entity's
/// animation, under a bone's <see cref="RemoteTransform2D"/> target or anywhere under the entity's body.
/// </summary>
/// <remarks>
/// <para>Put it on a layer of its own, which the entity's movement doesn't collide with and shots do. A shot that hits
/// it reports its <see cref="Tags"/> and has its damage scaled by <see cref="DamageScale"/>. A
/// <see cref="ForgeHitboxes2D"/> on the entity rewinds it for lag compensation, as the entity's body is.</para>
/// </remarks>
[GlobalClass]
public partial class ForgeHitbox2D : StaticBody2D
{
	/// <summary>
	/// Gets or sets the tags a hit on the part reports, such as one for a head.
	/// </summary>
	[Export]
	public ForgeTagContainer? Tags { get; set; }

	/// <summary>
	/// Gets or sets how much a hit on the part scales a shot's damage by: 2 for a head that takes double.
	/// </summary>
	[Export(PropertyHint.Range, "0,10,0.01,or_greater")]
	public float DamageScale { get; set; } = 1;

	/// <summary>
	/// Gets the tags a hit on the part reports.
	/// </summary>
	/// <returns>The tags, empty when none are set.</returns>
	public TagContainer GetTags()
	{
		return Tags?.GetTagContainer() ?? new TagContainer(Core.ForgeManagers.Instance.TagsManager);
	}
}
