// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SignedAngleResolverEditor : NodeEditorProperty
{
	private enum VectorSlot
	{
		From = 0,
		To = 1,
	}

	private static readonly Type[] _vectorExpectedTypes = [typeof(SysVector2), typeof(SysVector3)];
	private static readonly Type[] _axisExpectedTypes = [typeof(SysVector3)];

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private VBoxContainer? _fromEditorContainer;
	private VBoxContainer? _toEditorContainer;
	private VBoxContainer? _axisEditorContainer;
	private FoldableContainer? _fromFoldable;
	private FoldableContainer? _toFoldable;
	private FoldableContainer? _axisFoldable;
	private NodeEditorProperty? _fromEditor;
	private NodeEditorProperty? _toEditor;
	private NodeEditorProperty? _axisEditor;
	private CheckBox? _useAxisCheckBox;
	private bool _useAxis;
	private List<Func<NodeEditorProperty>> _vectorFactories = [];
	private List<Func<NodeEditorProperty>> _axisFactories = [];

	public override string DisplayName => "Signed Angle";

	public override string ResolverTypeId => "SignedAngle";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float) || expectedType == typeof(ForgeVariant128);
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
		_vectorFactories = ResolverEditorFactoryCatalog.GetCompatibleFactories(typeof(SysVector2), typeof(SysVector3));
		_axisFactories = ResolverEditorFactoryCatalog.GetCompatibleFactories(typeof(SysVector3));
		var existing = property?.Resolver as SignedAngleResolverResource;
		_useAxis = existing?.Axis is not null;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		BuildVectorSlot(
			root,
			VectorSlot.From,
			"From:",
			_vectorFactories,
			existing?.From,
			existing?.FromFolded ?? true,
			x => _fromEditor = x);

		BuildVectorSlot(
			root,
			VectorSlot.To,
			"To:",
			_vectorFactories,
			existing?.To,
			existing?.ToFolded ?? true,
			x => _toEditor = x);

		var axisToggleRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(axisToggleRow);
		_useAxisCheckBox = new CheckBox
		{
			Text = "Use Axis",
			ButtonPressed = _useAxis,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_useAxisCheckBox.Toggled += OnUseAxisToggled;
		axisToggleRow.AddChild(_useAxisCheckBox);

		_axisFoldable = CreateFoldable("Axis:", existing?.AxisFolded ?? true);
		_axisFoldable.Visible = _useAxis;
		root.AddChild(_axisFoldable);
		_axisEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_axisFoldable.AddChild(_axisEditorContainer);
		BuildAxisEditor(existing?.Axis);
		UpdateFoldableTitles();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new SignedAngleResolverResource
		{
			From = SaveNestedEditor(_fromEditor),
			FromFolded = _fromFoldable?.Folded ?? false,
			To = SaveNestedEditor(_toEditor),
			ToFolded = _toFoldable?.Folded ?? false,
			Axis = _useAxis ? SaveNestedEditor(_axisEditor) : null,
			AxisFolded = _axisFoldable?.Folded ?? false,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_fromEditor?.ClearCallbacks();
		_toEditor?.ClearCallbacks();
		_axisEditor?.ClearCallbacks();
		_fromEditorContainer = null;
		_toEditorContainer = null;
		_axisEditorContainer = null;
		_fromFoldable = null;
		_toFoldable = null;
		_axisFoldable = null;
		_fromEditor = null;
		_toEditor = null;
		_axisEditor = null;
		_useAxisCheckBox = null;
	}

	private static StatescriptResolverResource? SaveNestedEditor(NodeEditorProperty? editor)
	{
		if (editor is null)
		{
			return null;
		}

		var property = new StatescriptNodeProperty();
		editor.SaveTo(property);
		return property.Resolver;
	}

	private static void ClearEditorContainer(VBoxContainer container)
	{
		foreach (Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.Free();
		}
	}

	private static int GetSelectedIndex(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(factories, existingResolver, "Variant");
	}

	private static OptionButton CreateResolverDropdown(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (Func<NodeEditorProperty> factory in factories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		dropdown.Selected = GetSelectedIndex(factories, existingResolver);
		return dropdown;
	}

	private void BuildVectorSlot(
		VBoxContainer root,
		VectorSlot slot,
		string title,
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver,
		bool folded,
		Action<NodeEditorProperty?> setEditor)
	{
		FoldableContainer foldable = CreateFoldable(title, folded);

		if (slot == VectorSlot.From)
		{
			_fromFoldable = foldable;
		}
		else
		{
			_toFoldable = foldable;
		}

		root.AddChild(foldable);
		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(container);
		OptionButton dropdown = CreateResolverDropdown(factories, existingResolver);
		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		if (slot == VectorSlot.From)
		{
			_fromEditorContainer = editorContainer;
		}
		else
		{
			_toEditorContainer = editorContainer;
		}

		container.AddChild(dropdown);
		container.AddChild(editorContainer);

		ShowEditor(
			factories,
			GetSelectedIndex(factories, existingResolver),
			existingResolver,
			_vectorExpectedTypes,
			editorContainer,
			setEditor);

		if (slot == VectorSlot.From)
		{
			dropdown.ItemSelected += OnFromResolverDropdownItemSelected;
		}
		else
		{
			dropdown.ItemSelected += OnToResolverDropdownItemSelected;
		}
	}

	private void BuildAxisEditor(StatescriptResolverResource? existingResolver)
	{
		if (_axisEditorContainer is null)
		{
			return;
		}

		ClearEditorContainer(_axisEditorContainer);
		OptionButton dropdown = CreateResolverDropdown(_axisFactories, existingResolver);
		_axisEditorContainer.AddChild(dropdown);
		var nestedContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_axisEditorContainer.AddChild(nestedContainer);

		ShowEditor(
			_axisFactories,
			GetSelectedIndex(_axisFactories, existingResolver),
			existingResolver,
			_axisExpectedTypes,
			nestedContainer,
			x => _axisEditor = x);

		dropdown.ItemSelected += OnAxisResolverDropdownItemSelected;
	}

	private void OnFromResolverDropdownItemSelected(long index)
	{
		HandleVectorResolverChanged(_fromEditorContainer, _vectorFactories, (int)index, x => _fromEditor = x);
	}

	private void OnToResolverDropdownItemSelected(long index)
	{
		HandleVectorResolverChanged(_toEditorContainer, _vectorFactories, (int)index, x => _toEditor = x);
	}

	private void OnAxisResolverDropdownItemSelected(long index)
	{
		VBoxContainer? nestedContainer = GetAxisNestedContainer();
		if (nestedContainer is null)
		{
			return;
		}

		ClearEditorContainer(nestedContainer);
		_axisEditor = null;
		ShowEditor(_axisFactories, (int)index, null, _axisExpectedTypes, nestedContainer, x => _axisEditor = x);
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void HandleVectorResolverChanged(
		VBoxContainer? editorContainer,
		List<Func<NodeEditorProperty>> factories,
		int index,
		Action<NodeEditorProperty?> setEditor)
	{
		if (editorContainer is null)
		{
			return;
		}

		ClearEditorContainer(editorContainer);
		setEditor(null);
		ShowEditor(factories, index, null, _vectorExpectedTypes, editorContainer, setEditor);
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private VBoxContainer? GetAxisNestedContainer()
	{
		return _axisEditorContainer?.GetChildCount() > 1
			? _axisEditorContainer.GetChild(1) as VBoxContainer
			: null;
	}

	private FoldableContainer CreateFoldable(string title, bool folded)
	{
		var foldable = new FoldableContainer { Title = title, Folded = folded };
		foldable.FoldingChanged += OnFoldingChanged;
		return foldable;
	}

	private void OnFoldingChanged(bool isFolded)
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnUseAxisToggled(bool toggled)
	{
		_useAxis = toggled;
		if (_axisEditorContainer?.GetParent() is FoldableContainer axisFoldable)
		{
			axisFoldable.Visible = toggled;
		}

		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void ShowEditor(
		List<Func<NodeEditorProperty>> factories,
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		Type[] allowedExpectedTypes,
		VBoxContainer? container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || container is null || factoryIndex < 0 || factoryIndex >= factories.Count)
		{
			return;
		}

		NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
			_graph,
			factories,
			factoryIndex,
			existingResolver,
			allowedExpectedTypes,
			OnNestedEditorChanged,
			RaiseLayoutSizeChanged);

		if (editor is null)
		{
			return;
		}

		container.AddChild(editor);
		setEditor(editor);
	}

	private void OnNestedEditorChanged()
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
	}

	private void UpdateFoldableTitles()
	{
		if (_fromFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("From:", _fromFoldable, _fromEditor);
		}

		if (_toFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("To:", _toFoldable, _toEditor);
		}

		if (_axisFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Axis:", _axisFoldable, _axisEditor);
		}
	}
}
#endif
