// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that matches a full effect query against an active effect handle.
/// </summary>
[Tool]
internal sealed partial class EffectQueryMatchResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private ForgeEffectQuery? _query;
	private NestedResolverPicker? _handlePicker;

	public override string DisplayName => "Effect Query Match";

	public override string ResolverTypeId => "EffectQueryMatch";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as EffectQueryMatchResolverResource;
		_query = existingResource?.Query;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var queryPicker = new EditorResourcePicker
		{
			BaseType = nameof(ForgeEffectQuery),
			EditedResource = _query,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		queryPicker.ResourceChanged += resource =>
		{
			_query = resource as ForgeEffectQuery;
			_onChanged?.Invoke();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Query:", queryPicker, LabelWidth));

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
		property.Resolver = new EffectQueryMatchResolverResource
		{
			Query = _query,
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
