// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class TagQueryResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 66.0f;

	private ForgeQueryExpression? _query;
	private TagQuerySource _querySource = TagQuerySource.AllTags;

	private OptionButton? _sourceDropdown;
	private VBoxContainer? _queryEditorContainer;

	public override string DisplayName => "Tag Query";

	public override string ResolverTypeId => "TagQuery";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(Variant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var existingResource = property?.Resolver as TagQueryResolverResource;

		if (existingResource is not null)
		{
			_query = existingResource.Query ?? CreateDefaultQuery();
			_querySource = existingResource.QuerySource;
		}
		else
		{
			_query = CreateDefaultQuery();
		}

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_queryEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_queryEditorContainer);

		_sourceDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (TagQuerySource value in Enum.GetValues<TagQuerySource>())
		{
			_sourceDropdown.AddItem(value.ToString());
		}

		_sourceDropdown.Selected = (int)_querySource;
		_sourceDropdown.ItemSelected += OnSourceChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Source:", _sourceDropdown, LabelWidth));

		root.AddChild(CreateEntitySelectorRow(LabelWidth));
		root.AddChild(CreateEntityScopeEditorRow(LabelWidth));
		RefreshQueryEditor();
		PopulateEntityScopeEditor(existingResource?.EntityResolver);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new TagQueryResolverResource
		{
			Query = _query,
			QuerySource = _querySource,
			EntityResolver = BuildEntityResolverResource(),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _query?.ExpressionType.ToString() ?? "(None)";
		return true;
	}

	private static ForgeQueryExpression CreateDefaultQuery()
	{
		return new ForgeQueryExpression
		{
			ExpressionType = TagQueryExpressionType.AnyTagsMatch,
			TagContainer = new ForgeTagContainer(),
		};
	}

	private void OnSourceChanged(long index)
	{
		_querySource = (TagQuerySource)(int)index;
		NotifyChanged();
	}

	private void RefreshQueryEditor()
	{
		if (_queryEditorContainer is null)
		{
			return;
		}

		foreach (global::Godot.Node child in _queryEditorContainer.GetChildren())
		{
			_queryEditorContainer.RemoveChild(child);
			child.Free();
		}

		var editor = new QueryExpressionEditorControl();
		editor.Setup(_query ?? CreateDefaultQuery(), () =>
		{
			NotifyChanged();
			RaiseLayoutSizeChanged();
		});
		editor.LayoutChanged += RaiseLayoutSizeChanged;
		_queryEditorContainer.AddChild(editor);

		RaiseLayoutSizeChanged();
	}
}
#endif
