// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ElementObjectResolverEditor : NodeEditorProperty
{
	private string _objectTypeId = string.Empty;

	public override string DisplayName => "Element";

	public override string ResolverTypeId => "ElementObject";

	public override bool RequiresIterationScope => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		// Entity elements are authored through the dedicated Element Entity resolver instead.
		return expectedType != typeof(IForgeEntity)
			&& StatescriptObjectVariableTypeRegistry.IsObjectType(expectedType);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		if (StatescriptObjectVariableTypeRegistry.TryGetByClrType(
			expectedType,
			out StatescriptObjectVariableType? descriptor))
		{
			_objectTypeId = descriptor.TypeId;
		}
		else if (property?.Resolver is ElementObjectResolverResource existingResource)
		{
			_objectTypeId = existingResource.ObjectTypeId;
		}

		AddChild(new Label { Text = "Reads the iterated element." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ElementObjectResolverResource { ObjectTypeId = _objectTypeId };
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Element";
		return true;
	}
}
#endif
