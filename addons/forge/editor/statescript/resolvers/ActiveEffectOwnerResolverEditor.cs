// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the entity that triggered an active effect.
/// </summary>
[Tool]
internal sealed partial class ActiveEffectOwnerResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private NestedResolverPicker? _handlePicker;

	public override string DisplayName => "Active Effect Owner";

	public override string ResolverTypeId => "ActiveEffectOwner";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(IForgeEntity);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as ActiveEffectOwnerResolverResource;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_handlePicker = new NestedResolverPicker();
		_handlePicker.Initialize(
			graph,
			existingResource?.ActiveEffect,
			"Effect:",
			[typeof(ActiveEffectHandle)],
			isArray: false,
			folded: false,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_handlePicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ActiveEffectOwnerResolverResource
		{
			ActiveEffect = _handlePicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_handlePicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_handlePicker is not null && _handlePicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}
}
#endif
