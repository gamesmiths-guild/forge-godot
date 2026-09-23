// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class DegToRadResolverEditor : NumericOrVectorUnaryResolverEditorBase
{
	public override string DisplayName => "Deg To Rad";

	public override string ResolverTypeId => "DegToRad";

	protected override Type ResourceType => typeof(DegToRadResolverResource);
}
#endif
