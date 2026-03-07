// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
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

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(AttributeSetClass) || string.IsNullOrEmpty(AttributeName))
		{
			return;
		}

		var attributeKey = new StringKey($"{AttributeSetClass}.{AttributeName}");
		var propertyName = new StringKey($"__attr_{nodeId}_{index}");

		graph.VariableDefinitions.DefineProperty(propertyName, new AttributeResolver(attributeKey));

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		var attributeKey = new StringKey($"{AttributeSetClass}.{AttributeName}");
		return new AttributeResolver(attributeKey);
	}
}
