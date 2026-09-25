// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ASinHResolverEditor : ScalarUnaryResolverEditorBase
{
	public override string DisplayName => "ASinH";

	public override string ResolverTypeId => "ASinH";

	protected override Type ResourceType => typeof(ASinHResolverResource);
}
#endif
