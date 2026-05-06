// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ASinHResolverEditor : ScalarUnaryResolverEditorBase<ASinHResolverResource>
{
	public override string DisplayName => "ASinH";

	public override string ResolverTypeId => "ASinH";
}
#endif
