// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class Log10ResolverEditor : ScalarUnaryResolverEditorBase<Log10ResolverResource>
{
	public override string DisplayName => "Log10";

	public override string ResolverTypeId => "Log10";
}
#endif
