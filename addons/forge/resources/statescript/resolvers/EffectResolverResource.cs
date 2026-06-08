// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that authors one or more <see cref="Effect"/> instances for a node input. It combines a
/// <see cref="ForgeEffectData"/> selection with optional nested level and ownership resolvers and binds a core
/// <see cref="EffectFromDataResolver"/>/<see cref="EffectArrayFromDataResolver"/> at graph-build time.
/// </summary>
[Tool]
[GlobalClass]
public partial class EffectResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Effect";

	/// <summary>
	/// Gets or sets the selected scalar effect resource.
	/// </summary>
	[Export(PropertyHint.ResourceType, "ForgeEffectData")]
	public ForgeEffectData? Effect { get; set; }

	/// <summary>
	/// Gets or sets the selected effect resources when authoring an array input.
	/// </summary>
	[Export(PropertyHint.ResourceType, "ForgeEffectData")]
	public Array<ForgeEffectData> Effects { get; set; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether this resolver should bind an effect array.
	/// </summary>
	[Export]
	public bool IsArray { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver used for the effect level. When <see langword="null"/>, the effect falls back to
	/// the ability context level, or <c>1</c> without an ability context.
	/// </summary>
	[Export]
	public StatescriptResolverResource? LevelResolver { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver used for the effect ownership. When <see langword="null"/>, the effect falls back
	/// to the ability context ownership, or an empty ownership without an ability context.
	/// </summary>
	[Export]
	public OwnershipResolverResource? Ownership { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the effect-data editor section is folded.
	/// </summary>
	[Export]
	public bool EffectDataFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the level editor section is folded.
	/// </summary>
	[Export]
	public bool LevelFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the ownership editor section is folded.
	/// </summary>
	[Export]
	public bool OwnershipFolded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__effect_{nodeId}_{index}");
		IPropertyResolver? levelResolver = LevelResolver?.BuildResolver(graph);
		OwnershipResolver? ownershipResolver = Ownership?.BuildOwnershipResolver(graph);

		if (IsArray)
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(
				propertyName,
				new ForgeEffectArrayResolver(BuildEffectResources(), levelResolver, ownershipResolver));
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		if (Effect is null)
		{
			return;
		}

		graph.VariableDefinitions.DefineObjectProperty(
			propertyName,
			new ForgeEffectResolver(Effect, levelResolver, ownershipResolver));
		runtimeNode.BindInput(index, propertyName);
	}

	private List<ForgeEffectData> BuildEffectResources()
	{
		var effectResources = new List<ForgeEffectData>(Effects.Count);

		for (int i = 0; i < Effects.Count; i++)
		{
			if (Effects[i] is ForgeEffectData effectResource)
			{
				effectResources.Add(effectResource);
			}
		}

		return effectResources;
	}
}
