// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class DeltaAngleResolverEditor : ScalarBinaryResolverEditorBase<DeltaAngleResolverResource>
{
	public override string DisplayName => "Delta Angle";

	public override string ResolverTypeId => "DeltaAngle";

	protected override string LeftTitle => "Current:";

	protected override string RightTitle => "Target:";
}
#endif
