// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class LogResolverEditor : ScalarUnaryResolverEditorBase<LogResolverResource>
{
	public override string DisplayName => "Log";

	public override string ResolverTypeId => "Log";
}
#endif
