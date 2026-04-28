// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SqrtResolverEditor : NumericOrVectorUnaryResolverEditorBase<SqrtResolverResource>
{
	public override string DisplayName => "Sqrt";

	public override string ResolverTypeId => "Sqrt";
}
#endif
