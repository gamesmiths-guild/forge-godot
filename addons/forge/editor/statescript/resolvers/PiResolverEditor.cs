// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class PiResolverEditor : ScalarConstantResolverEditorBase<PiResolverResource>
{
	public override string DisplayName => "Pi";

	public override string ResolverTypeId => "Pi";
}
#endif
