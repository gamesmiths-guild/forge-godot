// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the direction the camera a graph is looking through faces.
/// </summary>
/// <remarks>
/// No rows: which camera is decided by the graph's own owner, so there is nothing to author.
/// </remarks>
[Tool]
internal sealed partial class CameraForward3DResolverEditor : NodeEditorProperty
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
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddChild(new Label { Text = "Uses the owner's active camera." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new CameraForward3DResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Camera Forward 3D";
		return true;
	}
}
#endif
