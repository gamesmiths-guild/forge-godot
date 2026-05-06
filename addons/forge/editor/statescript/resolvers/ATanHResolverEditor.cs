// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ATanHResolverEditor : ScalarUnaryResolverEditorBase<ATanHResolverResource>
{
	public override string DisplayName => "ATanH";

	public override string ResolverTypeId => "ATanH";
}
#endif
