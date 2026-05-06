// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class CrossResolverEditor : VectorBinaryResolverEditorBase<CrossResolverResource>
{
	public override string DisplayName => "Cross";

	public override string ResolverTypeId => "Cross";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector3) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
