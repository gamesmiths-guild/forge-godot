// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads a value from a Forge entity attribute at runtime.
/// </summary>
[Tool]
[GlobalClass]
public partial class AttributeResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Attribute";

	/// <summary>
	/// Gets or sets the attribute set class name.
	/// </summary>
	[Export]
	public string AttributeSetClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the attribute name within the attribute set.
	/// </summary>
	[Export]
	public string AttributeName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets which attribute value should be read.
	/// </summary>
	[Export]
	public AttributeCalculationType CalculationType { get; set; }

	/// <summary>
	/// Gets or sets the final channel to include when using
	/// <see cref="AttributeCalculationType.MagnitudeEvaluatedUpToChannel"/>.
	/// </summary>
	[Export]
	public int FinalChannel { get; set; }

	/// <summary>
	/// Gets or sets which entity should be inspected. Defaults to owner when omitted.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? EntityResolver { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(AttributeSetClass) || string.IsNullOrEmpty(AttributeName))
		{
			return;
		}

		var propertyName = new StringKey($"__attr_{nodeId}_{index}");

		graph.VariableDefinitions.DefineProperty(propertyName, BuildAttributeResolver(graph));

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return BuildAttributeResolver(graph);
	}

	private AttributeResolver BuildAttributeResolver(Graph graph)
	{
		var attributeKey = new StringKey($"{AttributeSetClass}.{AttributeName}");
		IEntityResolver entityResolver = EntityResolver?.BuildEntityResolver(graph) ?? new OwnerEntityResolver();
		return new AttributeResolver(attributeKey, entityResolver, CalculationType, FinalChannel);
	}
}
