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
/// Resolver resource that authors a constant <see cref="PackedScene"/> reference, or an array of them, for a node
/// input.
/// </summary>
/// <remarks>
/// Node settings only carry primitives, so this resolver is the single supported way to hand a scene to a graph. In
/// array shape it pairs with the core random-element and element-at resolvers to pick one of several scenes at runtime.
/// </remarks>
[Tool]
[GlobalClass]
public partial class ScenePickerResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ScenePicker";

	/// <summary>
	/// Gets or sets the selected scene when authoring a scalar input.
	/// </summary>
	[Export]
	public PackedScene? Scene { get; set; }

	/// <summary>
	/// Gets or sets the selected scenes when authoring an array input.
	/// </summary>
	[Export]
	public Array<PackedScene> Scenes { get; set; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether this resolver should bind a scene array.
	/// </summary>
	[Export]
	public bool IsArray { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__scene_{nodeId}_{index}");

		if (IsArray)
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(
				propertyName,
				new SceneArrayResolver(BuildSceneList()));
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		if (Scene is null)
		{
			return;
		}

		graph.VariableDefinitions.DefineObjectProperty(propertyName, new SceneResolver(Scene));
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (IsArray || Scene is null)
		{
			return false;
		}

		objectResolver = new SceneResolver(Scene);
		return true;
	}

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (!IsArray)
		{
			return false;
		}

		objectArrayResolver = new SceneArrayResolver(BuildSceneList());
		return true;
	}

	private List<PackedScene?> BuildSceneList()
	{
		var scenes = new List<PackedScene?>(Scenes.Count);

		for (int i = 0; i < Scenes.Count; i++)
		{
			scenes.Add(Scenes[i]);
		}

		return scenes;
	}
}
