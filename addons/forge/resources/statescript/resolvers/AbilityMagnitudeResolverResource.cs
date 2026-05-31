// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the ability activation magnitude from the <see cref="GraphContext.ActivationContext"/>
/// at runtime. Produces a <see langword="float"/> value.
/// </summary>
[Tool]
[GlobalClass]
public partial class AbilityMagnitudeResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityMagnitude";

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__mag_{nodeId}_{index}");

		graph.VariableDefinitions.DefineProperty(propertyName, new AbilityMagnitudeResolver());

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new AbilityMagnitudeResolver();
	}
}
