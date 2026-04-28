// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class NumericUnaryResolverEditorBase<TResource> : UnaryNestedResolverEditorBase<TResource>
	where TResource : UnaryNestedResolverResourceBase, new()
{
	protected override Type[] FactoryExpectedTypes => [typeof(int), typeof(float), typeof(double)];

	protected override Type NestedExpectedType => typeof(ForgeVariant128);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(int) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
