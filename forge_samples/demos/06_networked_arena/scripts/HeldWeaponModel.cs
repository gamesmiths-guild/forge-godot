// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Shows the model of the weapon an entity holds: each child for the tag at its place in <see cref="WeaponTags"/>. The
/// tags are the weapon abilities' activation owned tags, which reach every client, so everyone sees what everyone
/// holds.
/// </summary>
public partial class HeldWeaponModel : Node3D
{
	/// <summary>
	/// Gets or sets the entity whose weapon to show.
	/// </summary>
	[Export]
	public required ForgeEntity Entity { get; set; }

	/// <summary>
	/// Gets or sets each child's tag, in the children's order.
	/// </summary>
	[Export]
	public Array<ForgeTag> WeaponTags { get; set; } = [];

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		for (int i = 0; i < WeaponTags.Count && i < GetChildCount(); i++)
		{
			if (GetChild(i) is Node3D model)
			{
				model.Visible = Entity.Tags.AllTags.HasTag(WeaponTags[i].GetTag());
			}
		}
	}
}
