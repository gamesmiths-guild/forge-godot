// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class CosResolverEditor : ScalarUnaryResolverEditorBase
{
	public override string DisplayName => "Cos";

	public override string ResolverTypeId => "Cos";

	protected override Type ResourceType => typeof(CosResolverResource);
}
#endif
