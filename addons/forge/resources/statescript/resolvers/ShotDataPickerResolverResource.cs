// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that authors a constant shot resource for a node input: a <see cref="ForgeProjectileData"/>, a
/// <see cref="ForgeSplashData"/> or a <see cref="ForgeHitscanData"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class ShotDataPickerResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ShotData";

	/// <summary>
	/// Gets or sets the resource.
	/// </summary>
	[Export]
	public Resource? Data { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (!TryBuildObjectResolver(graph, out IObjectResolver? resolver))
		{
			return;
		}

		var propertyName = new StringKey($"__shot_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, resolver!);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = Data switch
		{
			ForgeProjectileData projectile => new ConstantResourceResolver<ForgeProjectileData>(projectile),
			ForgeSplashData splash => new ConstantResourceResolver<ForgeSplashData>(splash),
			ForgeHitscanData hitscan => new ConstantResourceResolver<ForgeHitscanData>(hitscan),
			_ => null,
		};

		return objectResolver is not null;
	}
}
