// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that evaluates a tag query against an active effect's own tags, granted tags, or both.
/// </summary>
[Tool]
internal sealed partial class ActiveEffectTagQueryResolverEditor : NodeEditorProperty
{
	// Shared with the embedded expression editor so our rows line up with the ones it contributes.
	private const float LabelWidth = QueryExpressionEditorControl.LabelWidth;

	private Action? _onChanged;
	private ForgeQueryExpression? _query;
	private EffectTagSource _tagSource = EffectTagSource.OwningTags;
	private VBoxContainer? _queryEditorContainer;
	private NestedResolverPicker? _handlePicker;

	public override string DisplayName => "Active Effect Tag Query";

	public override string ResolverTypeId => "ActiveEffectTagQuery";

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

		var existingResource = property?.Resolver as ActiveEffectTagQueryResolverResource;
		_query = existingResource?.Query ?? CreateDefaultQuery();
		_tagSource = existingResource?.TagSource ?? EffectTagSource.OwningTags;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_queryEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_queryEditorContainer);

		var sourceDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (EffectTagSource value in Enum.GetValues<EffectTagSource>())
		{
			sourceDropdown.AddItem(value.ToString());
		}

		sourceDropdown.Selected = (int)_tagSource;
		sourceDropdown.ItemSelected += index =>
		{
			_tagSource = (EffectTagSource)(int)index;
			_onChanged?.Invoke();
		};

		// Kept short on purpose: a longer label overflows LabelWidth and shifts this row out of alignment.
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Set:", sourceDropdown, LabelWidth));

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

		RefreshQueryEditor();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ActiveEffectTagQueryResolverResource
		{
			Query = _query,
			TagSource = _tagSource,
			ActiveEffect = _handlePicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_handlePicker?.ClearCallbacks();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = $"{_tagSource}: {_query?.ExpressionType.ToString() ?? "(None)"}";
		return true;
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

	private static ForgeQueryExpression CreateDefaultQuery()
	{
		return new ForgeQueryExpression
		{
			ExpressionType = TagQueryExpressionType.AnyTagsMatch,
			TagContainer = new ForgeTagContainer(),
		};
	}

	private void RefreshQueryEditor()
	{
		if (_queryEditorContainer is null)
		{
			return;
		}

		foreach (Node child in _queryEditorContainer.GetChildren())
		{
			_queryEditorContainer.RemoveChild(child);
			child.Free();
		}

		var editor = new QueryExpressionEditorControl();
		editor.Setup(_query ?? CreateDefaultQuery(), () =>
		{
			_onChanged?.Invoke();
			RaiseLayoutSizeChanged();
		});
		editor.LayoutChanged += RaiseLayoutSizeChanged;
		_queryEditorContainer.AddChild(editor);

		RaiseLayoutSizeChanged();
	}
}
#endif
