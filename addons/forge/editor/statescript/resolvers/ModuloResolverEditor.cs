// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ModuloResolverEditor : BinaryNestedResolverEditorBase<ModuloResolverResource>
{
	public override string DisplayName => "Modulo";

	public override string ResolverTypeId => "Modulo";

	protected override Type[] FactoryExpectedTypes => [typeof(int), typeof(float), typeof(double)];

	protected override Type NestedExpectedType => typeof(ForgeVariant128);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128) || ResolverEditorCompatibility.IsNumericType(expectedType);
	}
}
#endif
