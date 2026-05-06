// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

[Tool]
internal abstract partial class RandomInfoResolverEditorBase : NodeEditorProperty
{
	protected abstract string InfoText { get; }

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddChild(new Label
		{
			Text = InfoText,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});
	}
}
#endif
