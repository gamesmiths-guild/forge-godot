// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the live <see cref="Effect"/> instance behind an <see cref="ActiveEffectHandle"/>
/// produced by a nested resolver — bridging the handle lane back to the effect lane.
/// </summary>
[Tool]
[GlobalClass]
public partial class ActiveEffectEffectResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ActiveEffectEffect";

	/// <summary>
	/// Gets or sets the nested resolver providing the active effect handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? ActiveEffect { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (!TryBuildObjectResolver(graph, out IObjectResolver? objectResolver) || objectResolver is null)
		{
			return;
		}

		var propertyName = new StringKey($"__activeeffecteffect_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, objectResolver);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (ActiveEffect is null
			|| !ActiveEffect.TryBuildObjectResolver(graph, out IObjectResolver? handleResolver)
			|| handleResolver is not IObjectResolver<ActiveEffectHandle> typedHandleResolver)
		{
			GD.PushError("Statescript: Active Effect's Effect resolver requires an Active Effect source.");
			return false;
		}

		objectResolver = new ActiveEffectEffectResolver(typedHandleResolver);
		return true;
	}
}
