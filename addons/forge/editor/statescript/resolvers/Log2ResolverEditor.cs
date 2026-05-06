// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class Log2ResolverEditor : ScalarUnaryResolverEditorBase<Log2ResolverResource>
{
	public override string DisplayName => "Log2";

	public override string ResolverTypeId => "Log2";
}
#endif
