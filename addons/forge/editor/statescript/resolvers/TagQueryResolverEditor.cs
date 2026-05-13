// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
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
internal sealed partial class TagQueryResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 66.0f;

	private enum EntitySelection
	{
		Owner = 0,
		Source = 1,
		Target = 2,
		Variable = 3,
	}

	private ForgeQueryExpression? _query;
	private TagQuerySource _querySource = TagQuerySource.AllTags;
	private EntitySelection _entitySelection = EntitySelection.Owner;
	private Action? _onChanged;
	private StatescriptGraph? _graph;

	private OptionButton? _sourceDropdown;
	private OptionButton? _entityDropdown;
	private VBoxContainer? _queryEditorContainer;
	private Control? _entityEditorRow;
	private VBoxContainer? _entityEditorContainer;
	private EntityVariableResolverEditor? _entityVariableEditor;

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
		_graph = graph;
		_onChanged = onChanged;

		if (property?.Resolver is TagQueryResolverResource resource)
		{
			_query = resource.Query ?? CreateDefaultQuery();
			_querySource = resource.QuerySource;
			_entitySelection = ResolveEntitySelection(resource.EntityResolver);
		}
		else
		{
			_query = CreateDefaultQuery();
		}

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
		root.AddChild(CreateLabeledRow("Source:", _sourceDropdown));

		_entityDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_entityDropdown.AddItem("Owner");
		_entityDropdown.AddItem("Source");
		_entityDropdown.AddItem("Target");
		_entityDropdown.AddItem("Variable");
		_entityDropdown.Selected = (int)_entitySelection;
		_entityDropdown.ItemSelected += OnEntityChanged;
		root.AddChild(CreateLabeledRow("Entity:", _entityDropdown));

		_entityEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_entityEditorRow = CreateIndentedRow(_entityEditorContainer);
		root.AddChild(_entityEditorRow);
		RefreshQueryEditor();
		RebuildEntityEditor(property?.Resolver as TagQueryResolverResource);
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

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_entityVariableEditor?.ClearCallbacks();
	}

	private static EntitySelection ResolveEntitySelection(EntityResolverResourceBase? resource)
	{
		return resource switch
		{
			SourceEntityResolverResource => EntitySelection.Source,
			TargetEntityResolverResource => EntitySelection.Target,
			EntityVariableResolverResource => EntitySelection.Variable,
			_ => EntitySelection.Owner,
		};
	}

	private static HBoxContainer CreateLabeledRow(string labelText, Control editor)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(LabelWidth, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});
		row.AddChild(editor);
		return row;
	}

	private static HBoxContainer CreateIndentedRow(Control editor)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			CustomMinimumSize = new Vector2(LabelWidth, 0),
		});
		row.AddChild(editor);
		return row;
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
		_onChanged?.Invoke();
	}

	private void OnEntityChanged(long index)
	{
		_entitySelection = (EntitySelection)(int)index;
		RebuildEntityEditor(null);
		_onChanged?.Invoke();
	}

	private void RebuildEntityEditor(TagQueryResolverResource? existingResource)
	{
		if (_entityEditorContainer is null)
		{
			return;
		}

		foreach (global::Godot.Node child in _entityEditorContainer.GetChildren())
		{
			_entityEditorContainer.RemoveChild(child);
			child.Free();
		}

		_entityVariableEditor = null;

		if (_entitySelection != EntitySelection.Variable || _graph is null)
		{
			if (_entityEditorRow is not null)
			{
				_entityEditorRow.Visible = false;
			}

			RaiseLayoutSizeChanged();
			return;
		}

		StatescriptNodeProperty? entityProperty = existingResource?.EntityResolver
			is EntityVariableResolverResource entityVariable
				? new StatescriptNodeProperty { Resolver = entityVariable }
				: null;

		_entityVariableEditor = new EntityVariableResolverEditor();
		_entityVariableEditor.Setup(
			_graph,
			entityProperty,
			typeof(IForgeEntity),
			() =>
			{
				_onChanged?.Invoke();
				RaiseLayoutSizeChanged();
			},
			false);
		_entityVariableEditor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		_entityEditorContainer.AddChild(_entityVariableEditor);

		if (_entityEditorRow is not null)
		{
			_entityEditorRow.Visible = true;
		}

		RaiseLayoutSizeChanged();
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
			_onChanged?.Invoke();
			RaiseLayoutSizeChanged();
		});
		editor.LayoutChanged += RaiseLayoutSizeChanged;
		_queryEditorContainer.AddChild(editor);

		RaiseLayoutSizeChanged();
	}

	private EntityResolverResourceBase BuildEntityResolverResource()
	{
		return _entitySelection switch
		{
			EntitySelection.Source => new SourceEntityResolverResource(),
			EntitySelection.Target => new TargetEntityResolverResource(),
			EntitySelection.Variable => BuildEntityVariableResolver(),
			_ => new OwnerEntityResolverResource(),
		};
	}

	private EntityResolverResourceBase BuildEntityVariableResolver()
	{
		if (_entityVariableEditor is null)
		{
			return new EntityVariableResolverResource();
		}

		var tempProperty = new StatescriptNodeProperty();
		_entityVariableEditor.SaveTo(tempProperty);
		return (EntityVariableResolverResource?)tempProperty.Resolver ?? new EntityVariableResolverResource();
	}
}
#endif
