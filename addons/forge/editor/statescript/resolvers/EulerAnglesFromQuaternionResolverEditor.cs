// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeEulerOrder = Gamesmiths.Forge.Statescript.Properties.EulerOrder;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using GodotVector2 = Godot.Vector2;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class EulerAnglesFromQuaternionResolverEditor : NodeEditorProperty
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private VBoxContainer? _editorContainer;
	private NodeEditorProperty? _quaternionEditor;
	private List<Func<NodeEditorProperty>> _factories = [];
	private ForgeEulerOrder _order = ForgeEulerOrder.XYZ;

	public override string DisplayName => "Euler Angles From Quaternion";

	public override string ResolverTypeId => "EulerAnglesFromQuaternion";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector3) || expectedType == typeof(ForgeVariant128);
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
		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(typeof(SysQuaternion));
		var existing = property?.Resolver as EulerAnglesFromQuaternionResolverResource;
		_order = existing?.Order ?? ForgeEulerOrder.XYZ;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		FoldableContainer foldable = CreateFoldable("Quaternion:");
		root.AddChild(foldable);
		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(container);

		OptionButton dropdown = CreateResolverDropdown(existing?.Operand);
		_editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(dropdown);
		container.AddChild(_editorContainer);
		ShowEditor(GetSelectedIndex(existing?.Operand), existing?.Operand);
		dropdown.ItemSelected += OnResolverDropdownItemSelected;

		var orderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(orderRow);
		orderRow.AddChild(new Label
		{
			Text = "Order:",
			CustomMinimumSize = new GodotVector2(55, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		var orderDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (ForgeEulerOrder value in Enum.GetValues<ForgeEulerOrder>())
		{
			orderDropdown.AddItem(value.ToString());
		}

		orderDropdown.Selected = (int)_order;
		orderDropdown.ItemSelected += OnOrderDropdownItemSelected;
		orderRow.AddChild(orderDropdown);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		StatescriptResolverResource? operand = null;
		if (_quaternionEditor is not null)
		{
			var operandProperty = new StatescriptNodeProperty();
			_quaternionEditor.SaveTo(operandProperty);
			operand = operandProperty.Resolver;
		}

		property.Resolver = new EulerAnglesFromQuaternionResolverResource
		{
			Operand = operand,
			Order = _order,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_quaternionEditor?.ClearCallbacks();
	}

	private FoldableContainer CreateFoldable(string title)
	{
		var foldable = new FoldableContainer { Title = title };
		foldable.FoldingChanged += _ => RaiseLayoutSizeChanged();
		return foldable;
	}

	private void OnResolverDropdownItemSelected(long index)
	{
		if (_editorContainer is null)
		{
			return;
		}

		foreach (Node child in _editorContainer.GetChildren())
		{
			_editorContainer.RemoveChild(child);
			child.Free();
		}

		_quaternionEditor = null;
		ShowEditor((int)index, null);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnOrderDropdownItemSelected(long index)
	{
		_order = (ForgeEulerOrder)(int)index;
		_onChanged?.Invoke();
	}

	private int GetSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(_factories, existingResolver, "Variant");
	}

	private OptionButton CreateResolverDropdown(StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (Func<NodeEditorProperty> factory in _factories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		dropdown.Selected = GetSelectedIndex(existingResolver);
		return dropdown;
	}

	private void ShowEditor(int factoryIndex, StatescriptResolverResource? existingResolver)
	{
		if (_graph is null || _editorContainer is null || factoryIndex < 0 || factoryIndex >= _factories.Count)
		{
			return;
		}

		NodeEditorProperty editor = _factories[factoryIndex]();
		StatescriptNodeProperty? tempProperty = existingResolver is null ? null : new StatescriptNodeProperty { Resolver = existingResolver };
		editor.Setup(_graph, tempProperty, typeof(SysQuaternion), OnNestedEditorChanged, false);
		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		_editorContainer.AddChild(editor);
		_quaternionEditor = editor;
	}

	private void OnNestedEditorChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
