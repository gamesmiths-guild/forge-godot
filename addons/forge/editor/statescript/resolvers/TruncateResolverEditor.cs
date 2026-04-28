// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class TruncateResolverEditor : FloatUnaryResolverEditorBase<TruncateResolverResource>
{
	public override string DisplayName => "Truncate";

	public override string ResolverTypeId => "Truncate";
}
#endif
