// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node property to a graph variable by name.
/// </summary>
[Tool]
[GlobalClass]
public partial class VariableResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// Gets or sets the name of the graph variable to bind to.
	/// </summary>
	[Export]
	public string VariableName { get; set; } = string.Empty;
}
