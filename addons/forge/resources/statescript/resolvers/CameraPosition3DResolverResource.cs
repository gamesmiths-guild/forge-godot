// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads where the camera a graph is looking through sits.
/// </summary>
[Tool]
[GlobalClass]
public partial class CameraPosition3DResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CameraPosition3D";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "cameraposition3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new CameraPosition3DResolver();
	}
}
