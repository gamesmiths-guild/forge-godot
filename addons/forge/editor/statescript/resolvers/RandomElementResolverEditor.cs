// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class RandomElementResolverEditor : ArrayReductionResolverEditorBase
{
	public override string DisplayName => "Random Element";

	public override string ResolverTypeId => "RandomElement";

	public override bool IsCompatibleWith(Type expectedType)
	{
		// Value-typed elements and object-backed elements (entities, handles) are both supported.
		return expectedType == typeof(ForgeVariant128)
			|| StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _)
			|| !expectedType.IsValueType;
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RandomElementResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
		};
	}

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as RandomElementResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as RandomElementResolverResource)?.SourceFolded ?? true;
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return GetAllowedExpectedTypes(expectedType);
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
