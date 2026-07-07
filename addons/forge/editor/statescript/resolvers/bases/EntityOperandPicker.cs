// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
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
	}

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Action? _layoutSizeChanged;
	private Control? _variableEditorRow;
	private VBoxContainer? _variableEditorContainer;
	private VariableResolverEditor? _variableEditor;
	private EntitySelection _selection = EntitySelection.Owner;

	public void Initialize(
		StatescriptGraph graph,
		EntityResolverResourceBase? existingResolver,
		string label,
		float labelWidth,
		Action onChanged,
		Action layoutSizeChanged,
		bool iterationScope = false)
	{
		_graph = graph;
		_onChanged = onChanged;
		_layoutSizeChanged = layoutSizeChanged;
		_selection = ResolveEntitySelection(existingResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		dropdown.AddItem("Owner");
		dropdown.AddItem("Source");
		dropdown.AddItem("Target");
		dropdown.AddItem("Variable");
		dropdown.AddItem("Element");

		// The iterated element only exists inside an array operation's per-element (lambda) operand.
		dropdown.SetItemDisabled((int)EntitySelection.Element, !iterationScope);

		dropdown.Selected = (int)_selection;
		dropdown.ItemSelected += OnEntityChanged;
		AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(label, dropdown, labelWidth));

		_variableEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_variableEditorRow = ResolverEditorLayoutUtilities.CreateIndentedRow(_variableEditorContainer, labelWidth);
		_variableEditorRow.Visible = false;
		AddChild(_variableEditorRow);

		RebuildVariableEditor(existingResolver);
	}

	public EntityResolverResourceBase BuildResource()
	{
		return _selection switch
		{
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

	private static EntitySelection ResolveEntitySelection(EntityResolverResourceBase? resource)
	{
		return resource switch
		{
			AbilitySourceResolverResource => EntitySelection.Source,
			AbilityTargetResolverResource => EntitySelection.Target,
			VariableResolverResource => EntitySelection.Variable,
			ElementEntityResolverResource => EntitySelection.Element,
			_ => EntitySelection.Owner,
		};
	}

	private void OnEntityChanged(long index)
	{
		_selection = (EntitySelection)(int)index;
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
