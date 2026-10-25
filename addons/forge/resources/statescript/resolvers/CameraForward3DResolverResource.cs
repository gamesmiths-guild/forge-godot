// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using InputAimResolver = Gamesmiths.Forge.Godot.Core.Statescript.Resolvers.InputAimResolver;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the direction the camera a graph is looking through faces.
/// </summary>
[Tool]
[GlobalClass]
public partial class CameraForward3DResolverResource : InputResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CameraForward3D";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "cameraforward3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new InputAimResolver(BuildInputSource(graph), new CameraForward3DResolver(), InputAimValue.Direction3D);
	}
}
