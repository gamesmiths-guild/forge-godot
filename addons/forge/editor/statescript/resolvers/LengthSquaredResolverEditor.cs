// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class LengthSquaredResolverEditor : VectorOrQuaternionUnaryFloatResolverEditorBase
{
	public override string DisplayName => "Length Squared";

	public override string ResolverTypeId => "LengthSquared";

	protected override Type ResourceType => typeof(LengthSquaredResolverResource);
}
#endif
