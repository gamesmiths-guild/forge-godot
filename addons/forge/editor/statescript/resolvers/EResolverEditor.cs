// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class EResolverEditor : ScalarConstantResolverEditorBase
{
	public override string DisplayName => "E";

	public override string ResolverTypeId => "E";

	protected override Type ResourceType => typeof(EResolverResource);
}
#endif
