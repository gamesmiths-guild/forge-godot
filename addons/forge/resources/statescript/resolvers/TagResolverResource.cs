// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that authors one or more tags for a tag input. The selected tags are bound as an object-array
/// property of <c>Tag</c>; a node that accepts a tag matrix iterates them as the tag side of its matrix.
/// </summary>
/// <remarks>
/// At graph-build time this binds a lazy <see cref="ForgeTagArrayResolver"/> so the tags are materialized from the
/// runtime tags manager only when resolved, never during editor-time graph builds.
/// </remarks>
[Tool]
[GlobalClass]
public partial class TagResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Tag";

	/// <summary>
	/// Gets or sets the selected tag keys.
	/// </summary>
	[Export]
	public Array<string> Tags { get; set; } = [];

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (Tags.Count == 0)
		{
			return;
		}

		var propertyName = new StringKey($"__tag_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, new ForgeTagArrayResolver([.. Tags]));
		runtimeNode.BindInput(index, propertyName);
	}
}
