// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// Interface that describes the fields available in a custom activation data type. Users implement this once per data
/// type to declare which fields the graph editor can bind to and which activation-data type the graph behavior should
/// use at runtime.
/// </summary>
/// <remarks>
/// <para>Implementations must expose <see cref="ActivationDataType"/> so Forge can construct the matching
/// <see cref="GraphAbilityBehavior{TData}"/>, and define <see cref="GetFields"/> to declare the available fields and
/// their types for the editor.</para>
/// <para>The provider is discovered automatically via reflection. Simply implement this interface and the class will
/// appear in the Activation Data resolver's provider dropdown in the graph editor.</para>
/// <para>A graph supports only one activation data provider at a time. If nodes in a graph already reference a
/// provider, the editor restricts subsequent nodes to the same provider. To use different activation data, define a
/// combined data type with all required fields.</para>
/// </remarks>
public interface IActivationDataProvider
{
	/// <summary>
	/// Gets the runtime activation-data type that should be used when constructing the graph behavior.
	/// </summary>
	Type ActivationDataType { get; }

	/// <summary>
	/// Returns the fields exposed by this activation data provider. Each entry defines a field name and type that graph
	/// nodes can read at runtime through the Activation Data resolver.
	/// </summary>
	/// <returns>An array of field definitions.</returns>
	ForgeActivationDataField[] GetFields();
}
