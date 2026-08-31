// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads one of the vectors a character body reports about its last move.
/// </summary>
[Tool]
[GlobalClass]
public partial class CharacterMotion2DResolverResource : SpatialResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CharacterMotion2D";

	/// <summary>
	/// Gets or sets which reading to report.
	/// </summary>
	[Export]
	public CharacterMotionValue Value { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "charactermotion2d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new CharacterMotion2DResolver(entityResolver, NodePath, Value);
	}
}
