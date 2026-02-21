
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads a value from a Forge entity attribute at runtime.
/// </summary>
[Tool]
[GlobalClass]
public partial class AttributeResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// Gets or sets the attribute set class name.
	/// </summary>
	[Export]
	public string AttributeSetClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the attribute name within the attribute set.
	/// </summary>
	[Export]
	public string AttributeName { get; set; } = string.Empty;
}
