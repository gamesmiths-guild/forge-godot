// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ASinResolverEditor : ScalarUnaryResolverEditorBase
{
	public override string DisplayName => "ASin";

	public override string ResolverTypeId => "ASin";

	protected override Type ResourceType => typeof(ASinResolverResource);
}
#endif
