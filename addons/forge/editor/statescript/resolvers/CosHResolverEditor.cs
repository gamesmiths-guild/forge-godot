// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class CosHResolverEditor : ScalarUnaryResolverEditorBase<CosHResolverResource>
{
	public override string DisplayName => "CosH";

	public override string ResolverTypeId => "CosH";
}
#endif
