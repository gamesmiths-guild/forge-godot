// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class FloorResolverEditor : FloatUnaryResolverEditorBase
{
	public override string DisplayName => "Floor";

	public override string ResolverTypeId => "Floor";

	protected override Type ResourceType => typeof(FloorResolverResource);
}
#endif
