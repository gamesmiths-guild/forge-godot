// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AndResolverEditor : BooleanBinaryResolverEditorBase
{
	public override string DisplayName => "And";

	public override string ResolverTypeId => "And";

	protected override Type ResourceType => typeof(AndResolverResource);
}
#endif
