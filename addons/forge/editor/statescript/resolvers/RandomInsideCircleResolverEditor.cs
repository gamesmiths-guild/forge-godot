// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class RandomInsideCircleResolverEditor : RandomInfoResolverEditorBase
{
	public override string DisplayName => "Random Inside Circle";

	public override string ResolverTypeId => "RandomInsideCircle";

	protected override string InfoText => "Random Point Inside Unit Circle";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector2) || expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RandomInsideCircleResolverResource();
	}
}
#endif
