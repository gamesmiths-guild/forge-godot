// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Reusable control that authors a single entity operand (Owner/Source/Target/Variable) for resolver editors that
/// need more than the one entity scope provided by <see cref="EntityScopedResolverEditorBase"/>.
/// </summary>
[Tool]
internal sealed partial class EntityOperandPicker : VBoxContainer
{
	private enum EntitySelection
	{
		Owner = 0,
		Source = 1,
		Target = 2,
		Variable = 3,
		Element = 4,
		None = 5,
	}

	private readonly List<EntitySelection> _dropdownSelections = [];

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Action? _layoutSizeChanged;
	private Control? _variableEditorRow;
	private VBoxContainer? _variableEditorContainer;
	private VariableResolverEditor? _variableEditor;
	private EntitySelection _selection = EntitySelection.Owner;

	/// <summary>
	/// Initializes the entity operand picker.
	/// </summary>
	/// <param name="graph">The graph the operand is authored against.</param>
	/// <param name="existingResolver">The stored operand, or <see langword="null"/> for a fresh one.</param>
	/// <param name="label">The row's display label.</param>
	/// <param name="labelWidth">The label column width.</param>
	/// <param name="onChanged">Invoked whenever the authored operand changes.</param>
	/// <param name="layoutSizeChanged">Invoked when the row's height changes.</param>
	/// <param name="iterationScope">Whether the iterated element is in scope (inside an array operation's lambda).
	/// </param>
	/// <param name="allowNone">Whether the operand may be left empty. Pass <see langword="true"/> only where an absent
	/// entity is a meaningful state distinct from every selectable entity — a source used to filter a lookup, where
	/// absent means "match any source". A None selection makes <see cref="BuildResource"/> return
	/// <see langword="null"/>, and an unset stored operand then reads back as None instead of falling back to Owner.
	/// </param>
	public void Initialize(
		StatescriptGraph graph,
		EntityResolverResourceBase? existingResolver,
		string label,
		float labelWidth,
		Action onChanged,
		Action layoutSizeChanged,
		bool iterationScope = false,
		bool allowNone = false)
	{
		_graph = graph;
		_onChanged = onChanged;
		_layoutSizeChanged = layoutSizeChanged;
		_selection = ResolveEntitySelection(existingResolver, allowNone);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		_dropdownSelections.Clear();

		if (allowNone)
		{
			AddSelectionItem(dropdown, EntitySelection.None, "None");
		}

		AddSelectionItem(dropdown, EntitySelection.Owner, "Owner");
		AddSelectionItem(dropdown, EntitySelection.Source, "Source");
		AddSelectionItem(dropdown, EntitySelection.Target, "Target");
		AddSelectionItem(dropdown, EntitySelection.Variable, "Variable");
		AddSelectionItem(dropdown, EntitySelection.Element, "Element");

		// The iterated element only exists inside an array operation's per-element (lambda) operand.
		dropdown.SetItemDisabled(_dropdownSelections.IndexOf(EntitySelection.Element), !iterationScope);

		dropdown.Selected = _dropdownSelections.IndexOf(_selection);
		dropdown.ItemSelected += OnEntityChanged;
		AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(label, dropdown, labelWidth));

		_variableEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_variableEditorRow = ResolverEditorLayoutUtilities.CreateIndentedRow(_variableEditorContainer, labelWidth);
		_variableEditorRow.Visible = false;
		AddChild(_variableEditorRow);

		RebuildVariableEditor(existingResolver);
	}

	/// <summary>
	/// Builds the authored operand, or <see langword="null"/> when the picker allows None and rests on it.
	/// </summary>
	/// <returns>The entity resolver resource, or <see langword="null"/> for None.</returns>
	public EntityResolverResourceBase? BuildResource()
	{
		return _selection switch
		{
			EntitySelection.None => null,
			EntitySelection.Source => new AbilitySourceResolverResource(),
			EntitySelection.Target => new AbilityTargetResolverResource(),
			EntitySelection.Variable => BuildVariableResolverResource(),
			EntitySelection.Element => new ElementEntityResolverResource(),
			_ => new AbilityOwnerResolverResource(),
		};
	}

	public void ClearCallbacks()
	{
		_onChanged = null;
		_layoutSizeChanged = null;
		_variableEditor?.ClearCallbacks();
	}

	private static EntitySelection ResolveEntitySelection(EntityResolverResourceBase? resource, bool allowNone)
	{
		return resource switch
		{
			AbilitySourceResolverResource => EntitySelection.Source,
			AbilityTargetResolverResource => EntitySelection.Target,
			VariableResolverResource => EntitySelection.Variable,
			ElementEntityResolverResource => EntitySelection.Element,
			null when allowNone => EntitySelection.None,
			_ => EntitySelection.Owner,
		};
	}

	private void AddSelectionItem(OptionButton dropdown, EntitySelection selection, string label)
	{
		dropdown.AddItem(label);
		_dropdownSelections.Add(selection);
	}

	private void OnEntityChanged(long index)
	{
		_selection = _dropdownSelections[(int)index];
		RebuildVariableEditor(null);
		_onChanged?.Invoke();
	}

	private void RebuildVariableEditor(EntityResolverResourceBase? existingResolver)
	{
		if (_variableEditorContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_variableEditorContainer);
		_variableEditor = null;

		if (_selection != EntitySelection.Variable || _graph is null)
		{
			if (_variableEditorRow is not null)
			{
				_variableEditorRow.Visible = false;
			}

			_layoutSizeChanged?.Invoke();
			return;
		}

		StatescriptNodeProperty? variableProperty = existingResolver is VariableResolverResource variableResolver
			? new StatescriptNodeProperty { Resolver = (StatescriptResolverResource)variableResolver.Duplicate() }
			: null;

		_variableEditor = new VariableResolverEditor();
		_variableEditor.Setup(
			_graph,
			variableProperty,
			typeof(IForgeEntity),
			() =>
			{
				_onChanged?.Invoke();
				_layoutSizeChanged?.Invoke();
			},
			false);
		_variableEditor.LayoutSizeChanged += () => _layoutSizeChanged?.Invoke();
		_variableEditorContainer.AddChild(_variableEditor);

		if (_variableEditorRow is not null)
		{
			_variableEditorRow.Visible = true;
		}

		_layoutSizeChanged?.Invoke();
	}

	private VariableResolverResource BuildVariableResolverResource()
	{
		if (_variableEditor is null)
		{
			return new VariableResolverResource { ObjectTypeId = "Entity" };
		}

		var tempProperty = new StatescriptNodeProperty();
		_variableEditor.SaveTo(tempProperty);

		if (tempProperty.Resolver is VariableResolverResource variableResolver)
		{
			return variableResolver;
		}

		return new VariableResolverResource { ObjectTypeId = "Entity" };
	}
}
#endif
