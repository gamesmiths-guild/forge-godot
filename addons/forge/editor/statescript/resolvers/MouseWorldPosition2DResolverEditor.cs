// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the point in the world the mouse cursor is over.
/// </summary>
/// <remarks>
/// No rows, unlike its 3D twin. A 2D cursor is already a point on the plane the game is played on, so there is no mode
/// to pick between, no ray to mask, and no reach to limit.
/// </remarks>
[Tool]
internal sealed partial class MouseWorldPosition2DResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Mouse World Position 2D";

	public override string ResolverTypeId => "MouseWorldPosition2D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(NumericsVector2) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddChild(new Label { Text = "Uses the owner's viewport." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new MouseWorldPosition2DResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Mouse World Position 2D";
		return true;
	}
}
#endif
