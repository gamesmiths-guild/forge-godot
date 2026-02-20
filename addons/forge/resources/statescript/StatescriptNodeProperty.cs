// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Resource representing a single node property binding. Stores the resolver resource reference.
/// </summary>
[Tool]
public partial class StatescriptNodeProperty : Resource
{
	/// <summary>
	/// Gets or sets the direction of this property (input or output).
	/// </summary>
	[Export]
	public StatescriptPropertyDirection Direction { get; set; }

	/// <summary>
	/// Gets or sets the index of this property in the node's InputProperties or OutputVariables array.
	/// </summary>
	[Export]
	public int PropertyIndex { get; set; }

	/// <summary>
	/// Gets or sets the resolver resource for this property.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Resolver { get; set; }
}
