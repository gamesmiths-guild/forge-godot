// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ExpResolverEditor : ScalarUnaryResolverEditorBase<ExpResolverResource>
{
	public override string DisplayName => "Exp";

	public override string ResolverTypeId => "Exp";
}
#endif
