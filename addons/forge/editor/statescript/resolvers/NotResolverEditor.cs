// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class NotResolverEditor : BooleanUnaryResolverEditorBase<NotResolverResource>
{
	public override string DisplayName => "Not";

	public override string ResolverTypeId => "Not";
}
#endif
