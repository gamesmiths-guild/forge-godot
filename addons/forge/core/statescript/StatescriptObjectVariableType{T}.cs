// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Strongly-typed base for an object-backed Statescript variable type. Provides all define/resolve plumbing via the
/// core generic APIs, so a concrete type only needs to supply <see cref="StatescriptObjectVariableType.TypeId"/> and
/// <see cref="StatescriptObjectVariableType.DisplayName"/>.
/// </summary>
/// <typeparam name="T">The reference type stored by variables of this type.</typeparam>
public abstract class StatescriptObjectVariableType<T> : StatescriptObjectVariableType
{
	/// <inheritdoc/>
	public sealed override Type ClrType => typeof(T);

	/// <inheritdoc/>
	public sealed override void DefineGraphVariable(GraphVariableDefinitions definitions, StringKey name)
	{
		definitions.DefineObjectVariable<T>(name);
	}

	/// <inheritdoc/>
	public sealed override void DefineGraphArrayVariable(GraphVariableDefinitions definitions, StringKey name)
	{
		definitions.DefineObjectArrayVariable<T>(name);
	}

	/// <inheritdoc/>
	public sealed override void DefineSharedVariable(Variables target, StringKey name)
	{
		target.DefineObjectVariable<T>(name);
	}

	/// <inheritdoc/>
	public sealed override void DefineSharedArrayVariable(Variables target, StringKey name)
	{
		target.DefineObjectArrayVariable(name, Array.Empty<T>());
	}

	/// <inheritdoc/>
	public sealed override IObjectResolver BuildVariableResolver(StringKey variableName, VariableScope scope)
	{
		return new ObjectVariableResolver<T>(variableName, scope);
	}

	/// <inheritdoc/>
	public sealed override IObjectArrayResolver BuildArrayVariableResolver(StringKey variableName, VariableScope scope)
	{
		return new ObjectArrayVariableResolver<T>(variableName, scope);
	}
}
