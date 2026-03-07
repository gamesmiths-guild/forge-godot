// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Base resource for all Statescript property resolvers. Each resolver type derives from this and implements the
/// binding methods to wire serialized editor data into the core runtime graph.
/// </summary>
[Tool]
[GlobalClass]
public partial class StatescriptResolverResource : Resource
{
	/// <summary>
	/// Binds this resolver as an input property on a runtime node. Implementations should register any necessary
	/// variable or property definitions on the graph and call <see cref="ForgeNode.BindInput"/> with the appropriate
	/// name.
	/// </summary>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="runtimeNode">The runtime node to bind the input on.</param>
	/// <param name="nodeId">The serialized node identifier, used for generating unique property names.</param>
	/// <param name="index">The zero-based index of the input property to bind.</param>
	public virtual void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
	}

	/// <summary>
	/// Binds this resolver as an output variable on a runtime node. Implementations should call
	/// <see cref="ForgeNode.BindOutput(byte, StringKey, VariableScope)"/> with the appropriate variable name.
	/// </summary>
	/// <param name="runtimeNode">The runtime node to bind the output on.</param>
	/// <param name="index">The zero-based index of the output variable to bind.</param>
	public virtual void BindOutput(ForgeNode runtimeNode, byte index)
	{
	}

	/// <summary>
	/// Creates an <see cref="IPropertyResolver"/> from this resolver resource. Used when this resolver appears as a
	/// nested operand inside another resolver (e.g., left/right side of a comparison).
	/// </summary>
	/// <param name="graph">The runtime graph being built, used for looking up variable type information.</param>
	/// <returns>The property resolver instance, or a default zero-value resolver if the resource is not configured.
	/// </returns>
	public virtual IPropertyResolver BuildResolver(Graph graph)
	{
		return new VariantResolver(default, typeof(int));
	}
}
