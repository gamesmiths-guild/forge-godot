// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Base for the resolvers that read a player's input: buttons, axes, the mouse and the aim.
/// </summary>
/// <remarks>
/// The input is read from <see cref="InputSource"/>, the ability's owner when unset. An entity with an input of its own
/// is read from that input; one without is read from the local device.
/// </remarks>
[Tool]
public abstract partial class InputResolverResourceBase : ValueResolverResourceBase
{
	/// <summary>
	/// Gets or sets the resolver for the entity whose input is read. Unset means the ability's owner.
	/// </summary>
	[Export]
	public StatescriptResolverResource? InputSource { get; set; }

	/// <summary>
	/// Builds the resolver for the entity whose input is read.
	/// </summary>
	/// <param name="graph">The graph being built.</param>
	/// <returns>The input source, or the ability's owner when unset.</returns>
	protected IEntityResolver BuildInputSource(Graph graph)
	{
		return EntityOperand.BuildOrOwner(InputSource, graph);
	}
}
