// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class TruncateResolverEditor : FloatUnaryResolverEditorBase
{
	public override string DisplayName => "Truncate";

	public override string ResolverTypeId => "Truncate";

	protected override Type ResourceType => typeof(TruncateResolverResource);
}
#endif
