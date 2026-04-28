// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysQuaternion = System.Numerics.Quaternion;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ConjugateResolverEditor : UnaryNestedResolverEditorBase<ConjugateResolverResource>
{
	public override string DisplayName => "Conjugate";

	public override string ResolverTypeId => "Conjugate";

	protected override Type[] FactoryExpectedTypes => [typeof(SysQuaternion)];

	protected override Type NestedExpectedType => typeof(SysQuaternion);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysQuaternion) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
