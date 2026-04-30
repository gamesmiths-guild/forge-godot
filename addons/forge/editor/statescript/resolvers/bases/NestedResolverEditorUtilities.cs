// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class NestedResolverEditorUtilities
{
	public static int GetSelectedIndex(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(factories, existingResolver, "Variant");
	}

	public static OptionButton CreateResolverDropdownControl(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

		foreach (Func<NodeEditorProperty> factory in factories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		dropdown.Selected = GetSelectedIndex(factories, existingResolver);
		return dropdown;
	}

	public static void ClearContainer(VBoxContainer? container)
	{
		if (container is null)
		{
			return;
		}

		foreach (Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.Free();
		}
	}

	public static Type[] ConstrainExpectedTypes(Type[] allowedExpectedTypes, Type[] candidateExpectedTypes)
	{
		if (allowedExpectedTypes.Length == 0)
		{
			return candidateExpectedTypes;
		}

		if (ContainsVariantType(allowedExpectedTypes))
		{
			return candidateExpectedTypes;
		}

		var constrainedExpectedTypes = new List<Type>();

		for (int i = 0; i < candidateExpectedTypes.Length; i++)
		{
			Type candidateExpectedType = candidateExpectedTypes[i];

			for (int j = 0; j < allowedExpectedTypes.Length; j++)
			{
				if (!AreExpectedTypesCompatible(candidateExpectedType, allowedExpectedTypes[j]))
				{
					continue;
				}

				if (!constrainedExpectedTypes.Contains(candidateExpectedType))
				{
					constrainedExpectedTypes.Add(candidateExpectedType);
				}

				break;
			}
		}

		return constrainedExpectedTypes.Count > 0 ? [.. constrainedExpectedTypes] : candidateExpectedTypes;
	}

	public static NodeEditorProperty? CreateNestedEditor(
		StatescriptGraph? graph,
		List<Func<NodeEditorProperty>> factories,
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		Type[] allowedExpectedTypes,
		Action onChanged,
		Action layoutSizeChanged)
	{
		if (graph is null || factoryIndex < 0 || factoryIndex >= factories.Count)
		{
			return null;
		}

		NodeEditorProperty editor = factories[factoryIndex]();
		Type[] compatibleExpectedTypes = GetCompatibleExpectedTypes(editor, allowedExpectedTypes);
		Type[] effectiveAllowedExpectedTypes = compatibleExpectedTypes.Length > 0
			? compatibleExpectedTypes
			: allowedExpectedTypes;

		editor.ConfigureAllowedExpectedTypes(effectiveAllowedExpectedTypes);

		StatescriptNodeProperty? tempProperty = existingResolver is null
			? null
			: new StatescriptNodeProperty { Resolver = existingResolver };

		editor.Setup(
			graph,
			tempProperty,
			GetEffectiveExpectedType(effectiveAllowedExpectedTypes),
			onChanged,
			false);
		editor.LayoutSizeChanged += layoutSizeChanged;
		return editor;
	}

	private static Type[] GetCompatibleExpectedTypes(NodeEditorProperty editor, Type[] allowedExpectedTypes)
	{
		var compatibleExpectedTypes = new List<Type>();

		for (int i = 0; i < allowedExpectedTypes.Length; i++)
		{
			Type allowedExpectedType = allowedExpectedTypes[i];

			if (editor.IsCompatibleWith(allowedExpectedType) && !compatibleExpectedTypes.Contains(allowedExpectedType))
			{
				compatibleExpectedTypes.Add(allowedExpectedType);
			}
		}

		return [.. compatibleExpectedTypes];
	}

	private static Type GetEffectiveExpectedType(Type[] allowedExpectedTypes)
	{
		return allowedExpectedTypes.Length == 1
			? allowedExpectedTypes[0]
			: typeof(ForgeVariant128);
	}

	private static bool AreExpectedTypesCompatible(Type candidateExpectedType, Type allowedExpectedType)
	{
		return candidateExpectedType == allowedExpectedType
			|| candidateExpectedType == typeof(ForgeVariant128)
			|| allowedExpectedType == typeof(ForgeVariant128)
			|| (ResolverEditorCompatibility.IsFloatType(candidateExpectedType)
				&& ResolverEditorCompatibility.IsFloatType(allowedExpectedType));
	}

	private static bool ContainsVariantType(Type[] expectedTypes)
	{
		for (int i = 0; i < expectedTypes.Length; i++)
		{
			if (expectedTypes[i] == typeof(ForgeVariant128))
			{
				return true;
			}
		}

		return false;
	}
}
#endif
