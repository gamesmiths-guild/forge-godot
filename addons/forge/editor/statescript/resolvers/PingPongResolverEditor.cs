// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class PingPongResolverEditor : ScalarBinaryResolverEditorBase<PingPongResolverResource>
{
	public override string DisplayName => "Ping Pong";

	public override string ResolverTypeId => "PingPong";

	protected override string LeftTitle => "Value:";

	protected override string RightTitle => "Length:";
}
#endif
