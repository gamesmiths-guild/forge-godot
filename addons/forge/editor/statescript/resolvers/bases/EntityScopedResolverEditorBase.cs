// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class EntityScopedResolverEditorBase : NodeEditorProperty
{
	protected enum EntitySelection
	{
		Owner = 0,
		Source = 1,
		Target = 2,
		Variable = 3,
	}

	private Action? _onChanged;
	private StatescriptGraph? _graph;
	private Control? _entityEditorRow;
	private VBoxContainer? _entityEditorContainer;
	private VariableResolverEditor? _entityVariableEditor;

	protected EntitySelection SelectedEntitySelection { get; private set; } = EntitySelection.Owner;

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_entityVariableEditor?.ClearCallbacks();
	}

	protected void InitializeEntityScope(
		StatescriptGraph graph,
		Action onChanged,
		EntityResolverResourceBase? entityResolver)
	{
		_graph = graph;
		_onChanged = onChanged;
		SelectedEntitySelection = ResolveEntitySelection(entityResolver);
	}

	protected HBoxContainer CreateEntitySelectorRow(float labelWidth)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		dropdown.AddItem("Owner");
		dropdown.AddItem("Source");
		dropdown.AddItem("Target");
		dropdown.AddItem("Variable");
		dropdown.Selected = (int)SelectedEntitySelection;
		dropdown.ItemSelected += OnEntityChanged;
		return ResolverEditorLayoutUtilities.CreateLabeledRow("Entity:", dropdown, labelWidth);
	}

	protected Control CreateEntityScopeEditorRow(float labelWidth)
	{
		_entityEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_entityEditorRow = ResolverEditorLayoutUtilities.CreateIndentedRow(_entityEditorContainer, labelWidth);
		_entityEditorRow.Visible = false;
		return _entityEditorRow;
	}

	protected void PopulateEntityScopeEditor(EntityResolverResourceBase? entityResolver)
	{
		RebuildEntityEditor(entityResolver);
	}

	protected void NotifyChanged()
	{
		_onChanged?.Invoke();
	}

	protected EntityResolverResourceBase BuildEntityResolverResource()
	{
		return SelectedEntitySelection switch
		{
			EntitySelection.Source => new SourceEntityResolverResource(),
			EntitySelection.Target => new TargetEntityResolverResource(),
			EntitySelection.Variable => BuildEntityVariableResolver(),
			_ => new OwnerEntityResolverResource(),
		};
	}

	private static EntitySelection ResolveEntitySelection(EntityResolverResourceBase? resource)
	{
		return resource switch
		{
			SourceEntityResolverResource => EntitySelection.Source,
			TargetEntityResolverResource => EntitySelection.Target,
			VariableResolverResource => EntitySelection.Variable,
			_ => EntitySelection.Owner,
		};
	}

	private void OnEntityChanged(long index)
	{
		SelectedEntitySelection = (EntitySelection)(int)index;
		RebuildEntityEditor(null);
		NotifyChanged();
	}

	private void RebuildEntityEditor(EntityResolverResourceBase? existingResolver)
	{
		if (_entityEditorContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_entityEditorContainer);
		_entityVariableEditor = null;

		if (SelectedEntitySelection != EntitySelection.Variable || _graph is null)
		{
			if (_entityEditorRow is not null)
			{
				_entityEditorRow.Visible = false;
			}

			RaiseLayoutSizeChanged();
			return;
		}

		StatescriptNodeProperty? entityProperty = existingResolver is VariableResolverResource entityVariable
			? new StatescriptNodeProperty { Resolver = (StatescriptResolverResource)entityVariable.Duplicate() }
			: null;

		_entityVariableEditor = new VariableResolverEditor();
		_entityVariableEditor.Setup(
			_graph,
			entityProperty,
			typeof(IForgeEntity),
			() =>
			{
				NotifyChanged();
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

	private VariableResolverResource BuildEntityVariableResolver()
	{
		if (_entityVariableEditor is null)
		{
			return new VariableResolverResource { VariableType = StatescriptVariableType.Entity };
		}

		var tempProperty = new StatescriptNodeProperty();
		_entityVariableEditor.SaveTo(tempProperty);
		if (tempProperty.Resolver is VariableResolverResource variableResolver)
		{
			return variableResolver;
		}

		return new VariableResolverResource { VariableType = StatescriptVariableType.Entity };
	}
}
#endif
