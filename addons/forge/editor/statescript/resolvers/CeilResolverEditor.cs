// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class CeilResolverEditor : FloatUnaryResolverEditorBase
{
	public override string DisplayName => "Ceil";

	public override string ResolverTypeId => "Ceil";

	protected override Type ResourceType => typeof(CeilResolverResource);
}
#endif
