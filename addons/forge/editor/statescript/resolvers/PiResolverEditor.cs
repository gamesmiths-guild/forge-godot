// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class PiResolverEditor : ScalarConstantResolverEditorBase
{
	public override string DisplayName => "Pi";

	public override string ResolverTypeId => "Pi";

	protected override Type ResourceType => typeof(PiResolverResource);
}
#endif
