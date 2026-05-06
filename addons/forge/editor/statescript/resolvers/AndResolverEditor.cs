// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AndResolverEditor : BooleanBinaryResolverEditorBase<AndResolverResource>
{
	public override string DisplayName => "And";

	public override string ResolverTypeId => "And";
}
#endif
