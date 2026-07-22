// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads a selected value (remaining duration, stack count, level, ...) from an
/// <see cref="ActiveEffectHandle"/> produced by a nested resolver — typically an Active Effect variable.
/// </summary>
[Tool]
[GlobalClass]
public partial class ActiveEffectDataResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ActiveEffectData";

	/// <summary>
	/// Gets or sets which value to read from the active effect.
	/// </summary>
	[Export]
	public ActiveEffectDataType DataType { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the active effect handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? ActiveEffect { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__activeeffectdata_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (ActiveEffect is null
			|| !ActiveEffect.TryBuildObjectResolver(graph, out IObjectResolver? handleResolver)
			|| handleResolver is not IObjectResolver<ActiveEffectHandle> typedHandleResolver)
		{
			GD.PushError("Statescript: Active Effect Data resolver requires an Active Effect source.");
			return new VariantResolver(default, typeof(int));
		}

		return new ActiveEffectDataResolver(typedHandleResolver, DataType);
	}
}
