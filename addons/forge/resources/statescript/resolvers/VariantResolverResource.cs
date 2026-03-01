// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that holds a constant (inline) value for a node property.
/// </summary>
[Tool]
[GlobalClass]
public partial class VariantResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// Gets or sets the constant value. Used when <see cref="IsArray"/> is <see langword="false"/>.
	/// </summary>
	[Export]
	public Variant Value { get; set; }

	/// <summary>
	/// Gets or sets the type interpretation for the value.
	/// </summary>
	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Int;

	/// <summary>
	/// Gets or sets a value indicating whether this resolver holds an array of values.
	/// </summary>
	[Export]
	public bool IsArray { get; set; }

	/// <summary>
	/// Gets or sets the array values. Used when <see cref="IsArray"/> is <see langword="true"/>.
	/// </summary>
	[Export]
	public Array<Variant> ArrayValues { get; set; } = [];
}
