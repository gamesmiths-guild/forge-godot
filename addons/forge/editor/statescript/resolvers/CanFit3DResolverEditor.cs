// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for whether a body would fit at a destination.
/// </summary>
[Tool]
internal sealed partial class CanFit3DResolverEditor : SpatialResolverEditorBase3D
{
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector3)];

	private NestedResolverPicker? _destinationPicker;

	public override string DisplayName => "Can Fit 3D";

	public override string ResolverTypeId => "CanFit3D";

	protected override Type ValueClrType => typeof(bool);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Can Fit 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_destinationPicker?.ClearCallbacks();
		_destinationPicker = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase3D? existingResource)
	{
		var resource = existingResource as CanFit3DResolverResource;

		// Seeded with a position resolver rather than left empty: a nested operand has no unbound state, so an
		// untouched destination would be the world origin, and "would I fit at the centre of the level" is a question
		// nobody is asking.
		_destinationPicker = new NestedResolverPicker();
#pragma warning disable SA1118 // Parameter should not span multiple lines
		_destinationPicker.Initialize(
			Graph!,
			resource?.Destination
				?? new EntityPosition3DResolverResource { EntityResolver = new AbilityTargetResolverResource() },
			"Destination:",
			_pointExpectedTypes,
			isArray: false,
			resource?.DestinationFolded ?? false,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
#pragma warning restore SA1118 // Parameter should not span multiple lines

		root.AddChild(_destinationPicker);
	}

	protected override SpatialResolverResourceBase3D BuildResource()
	{
		return new CanFit3DResolverResource
		{
			Destination = _destinationPicker?.BuildResource(),
			DestinationFolded = _destinationPicker?.Folded ?? false,
		};
	}
}
#endif
