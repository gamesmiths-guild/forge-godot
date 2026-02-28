// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that compares two nested numeric resolvers and produces a boolean result. Supports nesting any
/// numeric-compatible resolver as left/right operands, enabling powerful comparisons like "Attribute &gt; Constant".
/// </summary>
[Tool]
internal sealed partial class ComparisonResolverEditor : NodeEditorProperty
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;

	private OptionButton? _operationDropdown;
	private VBoxContainer? _leftContainer;
	private VBoxContainer? _rightContainer;
	private OptionButton? _leftResolverDropdown;
	private OptionButton? _rightResolverDropdown;

	private NodeEditorProperty? _leftEditor;
	private NodeEditorProperty? _rightEditor;

	private List<Func<NodeEditorProperty>> _numericFactories = [];
	private ComparisonOperation _operation;

	/// <inheritdoc/>
	public override string DisplayName => "Comparison";

	/// <inheritdoc/>
	public override Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public override string ResolverTypeId => "Comparison";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		_graph = graph;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		_numericFactories = StatescriptResolverRegistry.GetCompatibleFactories(typeof(ForgeVariant128));

		_numericFactories.RemoveAll(x =>
		{
			using NodeEditorProperty temp = x();
			return temp.ResolverTypeId == "Comparison";
		});

		var comparisonResolver = property?.Resolver as ComparisonResolverResource;

		if (comparisonResolver is not null)
		{
			_operation = comparisonResolver.Operation;
		}

		var leftFoldable = new FoldableContainer { Title = "Left:" };
		leftFoldable.FoldingChanged += _ => RaiseLayoutSizeChanged();
		vBox.AddChild(leftFoldable);

		_leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		leftFoldable.AddChild(_leftContainer);

		_leftResolverDropdown = CreateResolverDropdown(
			comparisonResolver?.Left,
			_leftContainer,
			x => _leftEditor = x);

		var opRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(opRow);

		opRow.AddChild(new Label { Text = "Op:" });

		_operationDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		_operationDropdown.AddItem("==");
		_operationDropdown.AddItem("!=");
		_operationDropdown.AddItem("<");
		_operationDropdown.AddItem("<=");
		_operationDropdown.AddItem(">");
		_operationDropdown.AddItem(">=");

		_operationDropdown.Selected = (int)_operation;

		_operationDropdown.ItemSelected += x =>
		{
			_operation = (ComparisonOperation)(int)x;
			_onChanged?.Invoke();
		};

		opRow.AddChild(_operationDropdown);

		var rightFoldable = new FoldableContainer { Title = "Right:" };
		rightFoldable.FoldingChanged += _ => RaiseLayoutSizeChanged();
		vBox.AddChild(rightFoldable);

		_rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		rightFoldable.AddChild(_rightContainer);

		_rightResolverDropdown = CreateResolverDropdown(
			comparisonResolver?.Right,
			_rightContainer,
			x => _rightEditor = x);
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		var comparisonResolver = new ComparisonResolverResource { Operation = _operation };

		if (_leftEditor is not null)
		{
			var leftProperty = new StatescriptNodeProperty();
			_leftEditor.SaveTo(leftProperty);
			comparisonResolver.Left = leftProperty.Resolver;
		}

		if (_rightEditor is not null)
		{
			var rightProperty = new StatescriptNodeProperty();
			_rightEditor.SaveTo(rightProperty);
			comparisonResolver.Right = rightProperty.Resolver;
		}

		property.Resolver = comparisonResolver;
	}

	private static string GetResolverTypeId(StatescriptResolverResource resolver)
	{
		return resolver switch
		{
			VariableResolverResource => "Variable",
			VariantResolverResource => "Variant",
			AttributeResolverResource => "Attribute",
			_ => string.Empty,
		};
	}

	private OptionButton CreateResolverDropdown(
		StatescriptResolverResource? existingResolver,
		VBoxContainer container,
		Action<NodeEditorProperty?> setEditor)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(dropdown);

		foreach (Func<NodeEditorProperty> factory in _numericFactories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		var selectedIndex = 0;

		if (existingResolver is not null)
		{
			var existingTypeId = GetResolverTypeId(existingResolver);

			for (var i = 0; i < _numericFactories.Count; i++)
			{
				using NodeEditorProperty temp = _numericFactories[i]();

				if (temp.ResolverTypeId == existingTypeId)
				{
					selectedIndex = i;
					break;
				}
			}
		}

		dropdown.Selected = selectedIndex;

		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(editorContainer);

		ShowNestedEditor(selectedIndex, existingResolver, editorContainer, setEditor);

		dropdown.ItemSelected += x =>
		{
			foreach (Node child in editorContainer.GetChildren())
			{
				editorContainer.RemoveChild(child);
				child.QueueFree();
			}

			setEditor(null);
			ShowNestedEditor((int)x, null, editorContainer, setEditor);
			_onChanged?.Invoke();
			RaiseLayoutSizeChanged();
		};

		return dropdown;
	}

	private void ShowNestedEditor(
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		VBoxContainer container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || factoryIndex < 0 || factoryIndex >= _numericFactories.Count)
		{
			return;
		}

		NodeEditorProperty editor = _numericFactories[factoryIndex]();

		StatescriptNodeProperty? tempProperty = null;

		if (existingResolver is not null)
		{
			tempProperty = new StatescriptNodeProperty { Resolver = existingResolver };
		}

		editor.Setup(_graph, tempProperty, typeof(ForgeVariant128), () => _onChanged?.Invoke());

		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;

		container.AddChild(editor);
		setEditor(editor);
	}
}
#endif
