// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads a cooldown value (remaining time, total time, remaining fraction) from an ability.
/// Defaults to the ability driving the graph; set an ability source to inspect a different ability.
/// </summary>
/// <remarks>
/// The optional cooldown tag is requested lazily on first resolve, so editor-time graph builds never touch the
/// runtime tags manager.
/// </remarks>
[Tool]
[GlobalClass]
public partial class AbilityCooldownResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityCooldown";

	/// <summary>
	/// Gets or sets which cooldown value to read.
	/// </summary>
	[Export]
	public AbilityCooldownDataType DataType { get; set; }

	/// <summary>
	/// Gets or sets the optional tag filtering which cooldown to read.
	/// </summary>
	[Export]
	public string CooldownTag { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the optional nested resolver providing the ability handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Ability { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__abilitycooldown_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		AbilityCooldownDataType dataType = DataType;
		string cooldownTag = CooldownTag;
		IObjectResolver<AbilityHandle>? handleResolver = AbilityResolverResourceUtilities.BuildHandleResolver(
			Ability,
			graph);

		return new LazyPropertyResolver(
			typeof(float),
			() =>
			{
				Tag? tag = string.IsNullOrEmpty(cooldownTag)
					? null
					: Tag.RequestTag(ForgeManagers.Instance.TagsManager, cooldownTag);

				return new AbilityCooldownResolver(dataType, tag, handleResolver);
			});
	}
}
