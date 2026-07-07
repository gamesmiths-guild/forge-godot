// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AverageResolverEditor : ArrayReductionResolverEditorBase
{
	public override string DisplayName => "Average";

	public override string ResolverTypeId => "Average";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return ArrayResolverEditorUtilities.IsNumericExpectedType(expectedType);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AverageResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
		};
	}

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as AverageResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as AverageResolverResource)?.SourceFolded ?? true;
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return [typeof(int), typeof(float), typeof(double)];
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
	}
}
#endif
