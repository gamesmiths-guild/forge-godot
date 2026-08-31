// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads how long an animation runs for, in seconds.
/// </summary>
[Tool]
[GlobalClass]
public partial class AnimationLengthResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AnimationLength";

	/// <summary>
	/// Gets or sets which entity's player to read. Defaults to the ability's owner when left unset.
	/// </summary>
	[Export]
	public StatescriptResolverResource? EntityResolver { get; set; }

	/// <summary>
	/// Gets or sets the path to the animation player, from the node the entity lives on. Empty means the entity's
	/// first animation player child.
	/// </summary>
	[Export]
	public string PlayerPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the name of the animation to measure.
	/// </summary>
	[Export]
	public string Animation { get; set; } = string.Empty;

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "animationlength";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new AnimationLengthResolver(
			EntityOperand.BuildOrOwner(EntityResolver, graph),
			PlayerPath,
			Animation);
	}
}
