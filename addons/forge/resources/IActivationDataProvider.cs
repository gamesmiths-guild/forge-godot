// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// Interface that describes the fields available in a custom activation data type. Users implement this once per data
/// type to declare which fields the graph editor can bind to, and to provide the runtime behavior that maps the data
/// into graph variables.
/// </summary>
/// <remarks>
/// <para>Implementations must define <see cref="GetFields"/> to declare the available fields and their types, and
/// <see cref="CreateBehavior"/> to produce a <see cref="GraphAbilityBehavior{TData}"/> with the appropriate data binder
/// that writes matching values into the graph's <see cref="Variables"/>.</para>
/// <para>The provider is discovered automatically via reflection. Simply implement this interface and the class will
/// appear in the Activation Data resolver's provider dropdown in the graph editor.</para>
/// <para>A graph supports only one activation data provider at a time. If nodes in a graph already reference a
/// provider, the editor restricts subsequent nodes to the same provider. To use different activation data, define a
/// combined data type with all required fields.</para>
/// </remarks>
public interface IActivationDataProvider
{
	/// <summary>
	/// Returns the fields exposed by this activation data provider. Each entry defines a field name and type that graph
	/// nodes can read at runtime through the Activation Data resolver.
	/// </summary>
	/// <returns>An array of field definitions.</returns>
	ForgeActivationDataField[] GetFields();

	/// <summary>
	/// Creates an <see cref="IAbilityBehavior"/> (typically a <see cref="GraphAbilityBehavior{TData}"/>) for the given
	/// graph. The returned behavior's data binder must write each declared field into the graph's
	/// <see cref="Variables"/> using matching names so that the Activation Data resolver can read them at runtime.
	/// </summary>
	/// <param name="graph">The runtime graph to execute.</param>
	/// <returns>An ability behavior that accepts the custom data type and maps it to graph variables.</returns>
	IAbilityBehavior CreateBehavior(Graph graph);
}
