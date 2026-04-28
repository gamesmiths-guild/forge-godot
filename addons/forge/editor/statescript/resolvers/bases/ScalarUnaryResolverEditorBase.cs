// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class ScalarUnaryResolverEditorBase<TResource> : UnaryNestedResolverEditorBase<TResource>
	where TResource : UnaryNestedResolverResourceBase, new()
{
	protected override Type[] FactoryExpectedTypes => [typeof(float), typeof(double)];

	protected override Type NestedExpectedType => typeof(float);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}
}
#endif
