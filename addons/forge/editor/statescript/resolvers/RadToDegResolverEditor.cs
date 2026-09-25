// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class RadToDegResolverEditor : NumericOrVectorUnaryResolverEditorBase
{
	public override string DisplayName => "Rad To Deg";

	public override string ResolverTypeId => "RadToDeg";

	protected override Type ResourceType => typeof(RadToDegResolverResource);
}
#endif
