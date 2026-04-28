// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ATan2ResolverEditor : ScalarBinaryResolverEditorBase<ATan2ResolverResource>
{
	public override string DisplayName => "ATan2";

	public override string ResolverTypeId => "ATan2";

	protected override string LeftTitle => "Y:";

	protected override string RightTitle => "X:";
}
#endif
