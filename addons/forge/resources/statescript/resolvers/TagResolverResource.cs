// Copyright © Gamesmiths Guild.

using Godot;

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
}
