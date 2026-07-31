// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class AttributeRequirements : ForgeEffectComponent
{
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
		return new AttributeRequirementsEffectComponent(
			Convert(ApplicationRequirements),
			Convert(RemovalRequirements),
			Convert(OngoingRequirements));
	}

	private static AttributeRequirement[] Convert(ForgeAttributeRequirement[] requirements)
	{
		List<AttributeRequirement> converted = [];

		foreach (ForgeAttributeRequirement requirement in requirements)
		{
			if (requirement is null)
			{
				continue;
			}

			converted.Add(requirement.GetAttributeRequirement());
		}

		return [.. converted];
	}
}
