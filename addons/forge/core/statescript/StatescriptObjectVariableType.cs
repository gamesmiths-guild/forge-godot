// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Describes an object-backed (reference) Statescript variable type so the editor and runtime can define, resolve, and
/// display variables of that type without hardcoding it.
/// </summary>
/// <remarks>
/// Object variable types are discovered automatically by <see cref="StatescriptObjectVariableTypeRegistry"/>. To add a
/// new object variable type, including in game code outside this plugin, derive from
/// <see cref="StatescriptObjectVariableType{T}"/> and provide a unique <see cref="TypeId"/> and a
/// <see cref="DisplayName"/>.
/// </remarks>
public abstract class StatescriptObjectVariableType
{
	/// <summary>
	/// Gets the stable, unique identifier persisted on variable definitions for this type (e.g., <c>"Entity"</c>).
	/// </summary>
	public abstract string TypeId { get; }

	/// <summary>
	/// Gets the designer-facing name shown in editor dropdowns.
	/// </summary>
	public abstract string DisplayName { get; }

	/// <summary>
	/// Gets the runtime CLR type stored by variables of this type.
	/// </summary>
	public abstract Type ClrType { get; }

	/// <summary>
	/// Defines a graph-scoped scalar variable of this type on the given definitions.
	/// </summary>
	/// <param name="definitions">The graph variable definitions to add to.</param>
	/// <param name="name">The variable name.</param>
	public abstract void DefineGraphVariable(GraphVariableDefinitions definitions, StringKey name);

	/// <summary>
	/// Defines a graph-scoped array variable of this type on the given definitions.
	/// </summary>
	/// <param name="definitions">The graph variable definitions to add to.</param>
	/// <param name="name">The variable name.</param>
	public abstract void DefineGraphArrayVariable(GraphVariableDefinitions definitions, StringKey name);

	/// <summary>
	/// Defines an entity-scoped (shared) scalar variable of this type on the given variables bag.
	/// </summary>
	/// <param name="target">The shared variables bag to add to.</param>
	/// <param name="name">The variable name.</param>
	public abstract void DefineSharedVariable(Variables target, StringKey name);

	/// <summary>
	/// Defines an entity-scoped (shared) array variable of this type on the given variables bag.
	/// </summary>
	/// <param name="target">The shared variables bag to add to.</param>
	/// <param name="name">The variable name.</param>
	public abstract void DefineSharedArrayVariable(Variables target, StringKey name);

	/// <summary>
	/// Builds a property resolver that reads a scalar variable of this type from the given scope.
	/// </summary>
	/// <param name="variableName">The variable to read.</param>
	/// <param name="scope">The scope to read from.</param>
	/// <returns>An object-backed resolver for the variable.</returns>
	public abstract IObjectResolver BuildVariableResolver(StringKey variableName, VariableScope scope);

	/// <summary>
	/// Builds a property resolver that reads an array variable of this type from the given scope.
	/// </summary>
	/// <param name="variableName">The variable to read.</param>
	/// <param name="scope">The scope to read from.</param>
	/// <returns>An object-backed array resolver for the variable.</returns>
	public abstract IObjectArrayResolver BuildArrayVariableResolver(StringKey variableName, VariableScope scope);

	/// <summary>
	/// Formats a runtime value of this type for debug output.
	/// </summary>
	/// <param name="value">The value to format, may be <see langword="null"/>.</param>
	/// <returns>A human-readable representation.</returns>
	public virtual string FormatDebugValue(object? value)
	{
		return value?.ToString() ?? "<null>";
	}
}
