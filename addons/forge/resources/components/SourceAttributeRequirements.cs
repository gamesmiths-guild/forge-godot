// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class SourceAttributeRequirements : ForgeEffectComponent
{
	[Export]
	public OwnershipEntity OwnershipEntity { get; set; }

	[ExportGroup("Application Requirements")]
	[Export]
	public ForgeAttributeRequirement[] ApplicationRequirements { get; set; } = [];

	[ExportGroup("Removal Requirements")]
	[Export]
	public ForgeAttributeRequirement[] RemovalRequirements { get; set; } = [];

	[ExportGroup("Ongoing Requirements")]
	[Export]
	public ForgeAttributeRequirement[] OngoingRequirements { get; set; } = [];

	public override IEffectComponent GetComponent()
	{
		return new SourceAttributeRequirementsEffectComponent(
			Convert(ApplicationRequirements),
			Convert(RemovalRequirements),
			Convert(OngoingRequirements),
			OwnershipEntity);
	}

	private static AttributeRequirement[] Convert(ForgeAttributeRequirement[] requirements)
	{
		List<AttributeRequirement> converted = [];

		foreach (ForgeAttributeRequirement requirement in requirements)
		{
			converted.Add(requirement.GetAttributeRequirement());
		}

		return [.. converted];
	}
}
