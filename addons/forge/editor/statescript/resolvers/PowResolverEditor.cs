// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class PowResolverEditor : NumericOrVectorBinaryResolverEditorBase
{
	public override string DisplayName => "Pow";

	public override string ResolverTypeId => "Pow";

	protected override Type ResourceType => typeof(PowResolverResource);

	protected override string LeftTitle => "Base:";

	protected override string RightTitle => "Exponent:";
}
#endif
