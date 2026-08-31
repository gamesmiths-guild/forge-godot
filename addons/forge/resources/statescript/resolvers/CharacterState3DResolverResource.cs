// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads whether a character body is touching the floor, a wall, or a ceiling.
/// </summary>
[Tool]
[GlobalClass]
public partial class CharacterState3DResolverResource : SpatialResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CharacterState3D";

	/// <summary>
	/// Gets or sets which contact state to report.
	/// </summary>
	[Export]
	public CharacterStateQuery State { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "characterstate3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new CharacterState3DResolver(entityResolver, NodePath, State);
	}
}
