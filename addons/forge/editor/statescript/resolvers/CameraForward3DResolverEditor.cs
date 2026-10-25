// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Editor for <see cref="CameraForward3DResolverResource"/>: its only row picks the entity whose input supplies the
/// aim.
/// </summary>
[Tool]
internal sealed partial class CameraForward3DResolverEditor : EntityScopedResolverEditorBase
{
	public override string DisplayName => "Camera Forward 3D";

	public override string ResolverTypeId => "CameraForward3D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(NumericsVector3) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		InitializeEntityScope(graph, onChanged, (property?.Resolver as CameraForward3DResolverResource)?.InputSource);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow("Input Source:"));
		root.AddChild(new Label { Text = "The input source's aim, or the owner's active camera without one." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new CameraForward3DResolverResource { InputSource = BuildEntityResolverResource() };
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Camera Forward 3D";
		return true;
	}
}
#endif
