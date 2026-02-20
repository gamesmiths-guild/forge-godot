// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that holds a constant (inline) value for a node property.
/// </summary>
[Tool]
public partial class VariantResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// Gets or sets the constant value.
	/// </summary>
	[Export]
	public Variant Value { get; set; }

	/// <summary>
	/// Gets or sets the type interpretation for the value.
	/// </summary>
	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Int;
}
