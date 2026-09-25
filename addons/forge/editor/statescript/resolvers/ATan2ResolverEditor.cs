// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ATan2ResolverEditor : ScalarBinaryResolverEditorBase
{
	public override string DisplayName => "ATan2";

	public override string ResolverTypeId => "ATan2";

	protected override Type ResourceType => typeof(ATan2ResolverResource);

	protected override string LeftTitle => "Y:";

	protected override string RightTitle => "X:";
}
#endif
