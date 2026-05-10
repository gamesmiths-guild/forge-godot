// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

internal sealed class NumericCoercionResolver(IPropertyResolver innerResolver, Type targetType) : IPropertyResolver
{
	private readonly IPropertyResolver _innerResolver = innerResolver;

	public Type ValueType { get; } = targetType;

	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 sourceValue = _innerResolver.Resolve(graphContext);
		return StatescriptNumericCompatibility.Coerce(sourceValue, _innerResolver.ValueType, ValueType);
	}
}
