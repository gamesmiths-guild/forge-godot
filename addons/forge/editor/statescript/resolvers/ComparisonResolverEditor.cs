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
		// Comparison produces a bool — compatible with bool and Variant128.
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

		// Get numeric-compatible resolver factories (for the nested operands).
		_numericFactories = StatescriptResolverRegistry.GetCompatibleFactories(typeof(ForgeVariant128));

		// Remove Comparison from the nested list to avoid infinite recursion.
		_numericFactories.RemoveAll(x =>
		{
			using NodeEditorProperty temp = x();
			return temp.ResolverTypeId == "Comparison";
		});

		// Restore from existing binding.
		var compRes = property?.Resolver as ComparisonResolverResource;

		if (compRes is not null)
		{
			_operation = compRes.Operation;
		}

		// Left operand.
		vBox.AddChild(new Label { Text = "Left:" });

		_leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(_leftContainer);

		_leftResolverDropdown = CreateResolverDropdown(
			compRes?.Left,
			_leftContainer,
			x => _leftEditor = x);

		// Operation dropdown.
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

		// Right operand.
		vBox.AddChild(new Label { Text = "Right:" });

		_rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(_rightContainer);

		_rightResolverDropdown = CreateResolverDropdown(
			compRes?.Right,
			_rightContainer,
			x => _rightEditor = x);
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		var compRes = new ComparisonResolverResource { Operation = _operation };

		if (_leftEditor is not null)
		{
			// Save through a temporary property to extract the resolver resource.
			var leftProp = new StatescriptNodeProperty();
			_leftEditor.SaveTo(leftProp);
			compRes.Left = leftProp.Resolver;
		}

		if (_rightEditor is not null)
		{
			var rightProp = new StatescriptNodeProperty();
			_rightEditor.SaveTo(rightProp);
			compRes.Right = rightProp.Resolver;
		}

		property.Resolver = compRes;
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

		// Determine initial selection.
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

		// Show the initial editor.
		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(editorContainer);

		ShowNestedEditor(selectedIndex, existingResolver, editorContainer, setEditor);

		dropdown.ItemSelected += x =>
		{
			// Clear old editor.
			foreach (Node child in editorContainer.GetChildren())
			{
				child.QueueFree();
			}

			setEditor(null);
			ShowNestedEditor((int)x, null, editorContainer, setEditor);
			_onChanged?.Invoke();
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

		// Create a temporary property to pass the existing resolver.
		StatescriptNodeProperty? tempProp = null;

		if (existingResolver is not null)
		{
			tempProp = new StatescriptNodeProperty { Resolver = existingResolver };
		}

		editor.Setup(_graph, tempProp, typeof(ForgeVariant128), () => _onChanged?.Invoke());
		container.AddChild(editor);
		setEditor(editor);
	}
}
#endif
