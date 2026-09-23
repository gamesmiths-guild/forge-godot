// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SinResolverEditor : ScalarUnaryResolverEditorBase
{
	public override string DisplayName => "Sin";

	public override string ResolverTypeId => "Sin";

	protected override Type ResourceType => typeof(SinResolverResource);
}
#endif
