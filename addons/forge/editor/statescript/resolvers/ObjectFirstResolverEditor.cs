// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ObjectFirstResolverEditor : ArrayReductionResolverEditorBase
{
	public override string DisplayName => "First";

	public override string ResolverTypeId => "ObjectFirst";

	public override bool IsCompatibleWith(Type expectedType)
	{
		// Entity arrays use the dedicated First Entity resolver (an entity resolver usable in attribute/tag reads).
		return expectedType != typeof(IForgeEntity)
			&& StatescriptObjectVariableTypeRegistry.IsObjectType(expectedType);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ObjectFirstResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
		};
	}

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as ObjectFirstResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as ObjectFirstResolverResource)?.SourceFolded ?? true;
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return [expectedType];
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
