// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SignResolverEditor : NumericUnaryResolverEditorBase
{
	public override string DisplayName => "Sign";

	public override string ResolverTypeId => "Sign";

	protected override Type ResourceType => typeof(SignResolverResource);
}
#endif
