// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

/// <summary>
/// Builds the activation data an ability bound to an input action activates with, from the same tick's input.
/// </summary>
/// <remarks>
/// Derive from it and return <c>AbilityInputBinding.ForAction&lt;TData&gt;(action, build, edge)</c>, where
/// <c>build</c> makes the data from the ability's owner and its input. Naming <c>TData</c> here, where it is known at
/// compile time, is what lets AOT builds activate the ability with typed data.
/// </remarks>
[Tool]
[GlobalClass]
public abstract partial class ForgeInputActivationData : Resource
{
	/// <summary>
	/// Creates the binding for an ability bound to an input action.
	/// </summary>
	/// <param name="action">The input action.</param>
	/// <param name="edge">The change of the action that activates the ability.</param>
	/// <returns>The binding.</returns>
	public abstract AbilityInputBinding CreateBinding(string action, InputEdge edge);
}
