// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that authors one or more <see cref="ForgeEffectData"/> resources for a node input.
/// It binds lazy runtime resolvers so editor-time graph validation does not eagerly materialize
/// <see cref="EffectData"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class EffectDataResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EffectData";

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

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__effect_{nodeId}_{index}");

		if (IsArray)
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, BuildForgeEffectArrayResolver());
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		if (Effect is null)
		{
			return;
		}

		graph.VariableDefinitions.DefineObjectProperty(
			propertyName,
			new ForgeEffectDataResolver(Effect));
		runtimeNode.BindInput(index, propertyName);
	}

	private ForgeEffectDataArrayResolver BuildForgeEffectArrayResolver()
	{
		var effectResources = new List<ForgeEffectData>(Effects.Count);

		for (int i = 0; i < Effects.Count; i++)
		{
			if (Effects[i] is ForgeEffectData effectResource)
			{
				effectResources.Add(effectResource);
			}
		}

		return new ForgeEffectDataArrayResolver(effectResources);
	}
}
