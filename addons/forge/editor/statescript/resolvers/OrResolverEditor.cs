// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class OrResolverEditor : BooleanBinaryResolverEditorBase
{
	public override string DisplayName => "Or";

	public override string ResolverTypeId => "Or";

	protected override Type ResourceType => typeof(OrResolverResource);
}
#endif
