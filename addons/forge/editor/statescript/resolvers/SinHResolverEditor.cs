// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SinHResolverEditor : ScalarUnaryResolverEditorBase<SinHResolverResource>
{
	public override string DisplayName => "SinH";

	public override string ResolverTypeId => "SinH";
}
#endif
