// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class BooleanBinaryResolverEditorBase<TResource> : BinaryNestedResolverEditorBase<TResource>
	where TResource : BinaryNestedResolverResourceBase, new()
{
	protected override Type[] FactoryExpectedTypes => [typeof(bool)];

	protected override Type NestedExpectedType => typeof(bool);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
