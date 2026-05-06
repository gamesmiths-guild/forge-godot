// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class RandomInsideSphereResolverEditor : RandomInfoResolverEditorBase
{
	public override string DisplayName => "Random Inside Sphere";

	public override string ResolverTypeId => "RandomInsideSphere";

	protected override string InfoText => "Random Point Inside Unit Sphere";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector3) || expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RandomInsideSphereResolverResource();
	}
}
#endif
