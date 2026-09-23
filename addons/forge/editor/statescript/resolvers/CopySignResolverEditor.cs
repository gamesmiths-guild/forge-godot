// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class CopySignResolverEditor : ScalarBinaryResolverEditorBase
{
	public override string DisplayName => "Copy Sign";

	public override string ResolverTypeId => "CopySign";

	protected override Type ResourceType => typeof(CopySignResolverResource);

	protected override string LeftTitle => "Magnitude:";

	protected override string RightTitle => "Sign:";
}
#endif
