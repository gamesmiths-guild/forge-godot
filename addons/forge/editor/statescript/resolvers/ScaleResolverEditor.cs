// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ScaleResolverEditor : ScaleResolverEditorBase
{
	public override string DisplayName => "Scale";

	public override string ResolverTypeId => "Scale";

	protected override Type ResourceType => typeof(ScaleResolverResource);
}
#endif
