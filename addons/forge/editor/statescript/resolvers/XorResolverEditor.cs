// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class XorResolverEditor : BooleanBinaryResolverEditorBase<XorResolverResource>
{
	public override string DisplayName => "Xor";

	public override string ResolverTypeId => "Xor";
}
#endif
