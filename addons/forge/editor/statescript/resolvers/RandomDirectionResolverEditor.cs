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
internal sealed partial class RandomDirectionResolverEditor : RandomInfoResolverEditorBase
{
	public override string DisplayName => "Random Direction";

	public override string ResolverTypeId => "RandomDirection";

	protected override string InfoText => "Random Unit Vector2";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector2) || expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RandomDirectionResolverResource();
	}
}
#endif
