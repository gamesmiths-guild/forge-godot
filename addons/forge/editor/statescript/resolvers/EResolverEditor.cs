// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class EResolverEditor : ScalarConstantResolverEditorBase<EResolverResource>
{
	public override string DisplayName => "E";

	public override string ResolverTypeId => "E";
}
#endif
