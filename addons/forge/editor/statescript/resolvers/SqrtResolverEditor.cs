// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SqrtResolverEditor : NumericOrVectorUnaryResolverEditorBase<SqrtResolverResource>
{
	public override string DisplayName => "Sqrt";

	public override string ResolverTypeId => "Sqrt";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(SysVector2)
			|| expectedType == typeof(SysVector3)
			|| expectedType == typeof(SysVector4)
			|| expectedType == typeof(ForgeVariant128);
	}
}
#endif
