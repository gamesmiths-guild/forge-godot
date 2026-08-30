// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the direction the camera a graph is looking through faces.
/// </summary>
[Tool]
[GlobalClass]
public partial class CameraForward3DResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CameraForward3D";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "cameraforward3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new CameraForward3DResolver();
	}
}
