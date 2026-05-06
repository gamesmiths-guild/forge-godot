// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ATanResolverEditor : ScalarUnaryResolverEditorBase<ATanResolverResource>
{
	public override string DisplayName => "ATan";

	public override string ResolverTypeId => "ATan";
}
#endif
