// Copyright © Gamesmiths Guild.

using System;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Groups a node type under a named subgroup in the editor's Add Node dialog.
/// </summary>
/// <remarks>
/// <para>The dialog's top level stays the Statescript archetype - Action, Condition, State, Flow - because that is what
/// determines how a node behaves in a graph. This attribute adds a second level <em>inside</em> an archetype for
/// grouping by subject matter, so that a few dozen scene, physics, and presentation nodes stay findable without
/// inventing new archetypes or colors.</para>
/// <para>Nodes without this attribute, which includes every node in the engine-agnostic core, are listed directly under
/// their archetype as before.</para>
/// </remarks>
/// <param name="category">The subgroup name, shown verbatim in the dialog.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StatescriptCategoryAttribute(string category) : Attribute
{
	/// <summary>
	/// Gets the subgroup name this node is listed under.
	/// </summary>
	public string Category { get; } = category;
}
