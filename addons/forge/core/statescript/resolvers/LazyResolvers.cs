// Copyright © Gamesmiths Guild.

#pragma warning disable SA1402, SA1649 // Single-type-per-file rules - the lazy wrappers form one tiny family.

using System;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Defers building a value-lane resolver until it is first resolved, so resolver resources that need runtime
/// managers (tags, effect data) never touch them during editor-time graph builds.
/// </summary>
/// <param name="valueType">The value type the deferred resolver produces.</param>
/// <param name="factory">The factory that builds the actual resolver on first resolve.</param>
internal sealed class LazyPropertyResolver(Type valueType, Func<IPropertyResolver> factory) : IPropertyResolver
{
	private readonly Func<IPropertyResolver> _factory = factory;

	private IPropertyResolver? _inner;

	public Type ValueType { get; } = valueType;

	public Variant128 Resolve(GraphContext graphContext)
	{
		_inner ??= _factory();
		return _inner.Resolve(graphContext);
	}
}

/// <summary>
/// Defers building an object-lane resolver until it is first resolved.
/// </summary>
/// <typeparam name="T">The value type to read.</typeparam>
/// <param name="factory">The factory that builds the actual resolver on first resolve.</param>
internal sealed class LazyObjectResolver<T>(Func<IObjectResolver<T>> factory) : ObjectResolver<T>
{
	private readonly Func<IObjectResolver<T>> _factory = factory;

	private IObjectResolver<T>? _inner;

	[return: MaybeNull]
	public override T Resolve(GraphContext graphContext)
	{
		_inner ??= _factory();
		return _inner.Resolve(graphContext);
	}
}

/// <summary>
/// Defers building an object-lane array resolver until it is first resolved.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="factory">The factory that builds the actual resolver on first resolve.</param>
internal sealed class LazyObjectArrayResolver<T>(Func<IObjectArrayResolver<T>> factory) : ObjectArrayResolver<T>
{
	private readonly Func<IObjectArrayResolver<T>> _factory = factory;

	private IObjectArrayResolver<T>? _inner;

	public override T[] ResolveArray(GraphContext graphContext)
	{
		_inner ??= _factory();
		return _inner.ResolveArray(graphContext);
	}
}
