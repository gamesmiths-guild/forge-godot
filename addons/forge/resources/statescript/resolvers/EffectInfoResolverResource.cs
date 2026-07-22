// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that computes an aggregate (total stacks, instance count, max level) over the active
/// applications of an effect on an entity — the "current number of stacks" query.
/// </summary>
/// <remarks>
/// The effect data is materialized lazily on first resolve, so editor-time graph builds never touch the runtime
/// managers.
/// </remarks>
[Tool]
[GlobalClass]
public partial class EffectInfoResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EffectInfo";

	/// <summary>
	/// Gets or sets the effect data to query for.
	/// </summary>
	[Export]
	public ForgeEffectData? EffectData { get; set; }

	/// <summary>
	/// Gets or sets which aggregate to compute.
	/// </summary>
	[Export]
	public EffectInfoType InfoType { get; set; }

	/// <summary>
	/// Gets or sets which entity should be inspected. Defaults to owner when omitted.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? EntityResolver { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__effectinfo_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (EffectData is null)
		{
			GD.PushError("Statescript: Effect Info resolver requires an effect data resource.");
			return new VariantResolver(default, typeof(int));
		}

		ForgeEffectData effectDataResource = EffectData;
		EffectInfoType infoType = InfoType;
		IEntityResolver entityResolver = EntityResolver?.BuildEntityResolver(graph) ?? new AbilityOwnerResolver();

		return new LazyPropertyResolver(
			typeof(int),
			() => new EffectInfoResolver(effectDataResource.GetEffectData(), infoType, entityResolver));
	}
}
