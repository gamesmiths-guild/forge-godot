// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether the owner entity has a given tag, resolving to a boolean value at runtime.
/// </summary>
[Tool]
[GlobalClass]
public partial class TagResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// Gets or sets the tag string to check for (e.g., "Status.Burning").
	/// </summary>
	[Export]
	public string Tag { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(Tag))
		{
			return;
		}

		var tag = Tags.Tag.RequestTag(ForgeManagers.Instance.TagsManager, Tag);
		var propertyName = new StringKey($"__tag_{nodeId}_{index}");

		graph.VariableDefinitions.DefineProperty(propertyName, new TagResolver(tag));

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(Tag))
		{
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		var tag = Tags.Tag.RequestTag(ForgeManagers.Instance.TagsManager, Tag);
		return new TagResolver(tag);
	}
}
