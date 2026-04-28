// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class TanHResolverEditor : ScalarUnaryResolverEditorBase<TanHResolverResource>
{
	public override string DisplayName => "TanH";

	public override string ResolverTypeId => "TanH";
}
#endif
