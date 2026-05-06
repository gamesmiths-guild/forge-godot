// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class LengthResolverEditor
	: VectorOrQuaternionUnaryFloatResolverEditorBase<LengthResolverResource>
{
	public override string DisplayName => "Length";

	public override string ResolverTypeId => "Length";
}
#endif
