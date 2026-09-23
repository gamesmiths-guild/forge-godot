// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ATanResolverEditor : ScalarUnaryResolverEditorBase
{
	public override string DisplayName => "ATan";

	public override string ResolverTypeId => "ATan";

	protected override Type ResourceType => typeof(ATanResolverResource);
}
#endif
