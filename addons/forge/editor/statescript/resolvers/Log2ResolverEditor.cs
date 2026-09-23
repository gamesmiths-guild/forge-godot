// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class Log2ResolverEditor : ScalarUnaryResolverEditorBase
{
	public override string DisplayName => "Log2";

	public override string ResolverTypeId => "Log2";

	protected override Type ResourceType => typeof(Log2ResolverResource);
}
#endif
