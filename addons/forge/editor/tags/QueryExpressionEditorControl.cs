// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class QueryExpressionEditorControl : VBoxContainer
{
	private const float LabelWidth = 66.0f;

	private ForgeQueryExpression? _query;
	private Action? _onChanged;
	private OptionButton? _expressionTypeDropdown;
	private VBoxContainer? _contentContainer;

	public event Action? LayoutChanged;

	public void Setup(ForgeQueryExpression query, Action onChanged)
	{
		_query = query;
		_onChanged = onChanged;
		EnsureDefaultData(query);

		if (IsNodeReady())
		{
			EnsureUi();
			RefreshUi();
		}
	}

	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		EnsureUi();
		RefreshUi();
	}

	public override void _ExitTree()
	{
		if (_expressionTypeDropdown is not null && IsInstanceValid(_expressionTypeDropdown))
		{
			_expressionTypeDropdown.ItemSelected -= OnExpressionTypeChanged;
		}

		LayoutChanged = null;
		_onChanged = null;
		_expressionTypeDropdown = null;
		_contentContainer = null;
		base._ExitTree();
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

	private static void ClearContainer(Control container)
	{
		foreach (Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.Free();
		}
	}

	private static void EnsureDefaultData(ForgeQueryExpression query)
	{
		if (query.ExpressionType == TagQueryExpressionType.Undefined)
		{
			return;
		}

		if (query.IsExpressionType())
		{
			query.Expressions ??= [];
			return;
		}

		query.TagContainer ??= new ForgeTagContainer();
	}

	private static ForgeQueryExpression CreateDefaultQueryExpression()
	{
		return new ForgeQueryExpression
		{
			ExpressionType = TagQueryExpressionType.AnyTagsMatch,
			TagContainer = new ForgeTagContainer(),
		};
	}

	private void EnsureUi()
	{
		if (_expressionTypeDropdown is not null && _contentContainer is not null)
		{
			return;
		}

		_expressionTypeDropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (TagQueryExpressionType value in Enum.GetValues<TagQueryExpressionType>())
		{
			_expressionTypeDropdown.AddItem(value.ToString());
		}

		_expressionTypeDropdown.ItemSelected += OnExpressionTypeChanged;
		AddChild(CreateLabeledRow("Type:", _expressionTypeDropdown));

		_contentContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(_contentContainer);
	}

	private void RefreshUi()
	{
		if (_query is null || _expressionTypeDropdown is null || _contentContainer is null)
		{
			return;
		}

		EnsureDefaultData(_query);
		_expressionTypeDropdown.Selected = (int)_query.ExpressionType;
		RefreshContent();
	}

	private void RefreshContent()
	{
		if (_query is null || _contentContainer is null)
		{
			return;
		}

		ClearContainer(_contentContainer);

		if (_query.ExpressionType == TagQueryExpressionType.Undefined)
		{
			_contentContainer.AddChild(new Label
			{
				Text = "Select an expression type.",
			});
			RaiseLayoutChanged();
			return;
		}

		if (_query.IsExpressionType())
		{
			BuildExpressionsEditor();
		}
		else
		{
			BuildTagContainerEditor();
		}

		RaiseLayoutChanged();
	}

	private void BuildExpressionsEditor()
	{
		if (_query is null || _contentContainer is null)
		{
			return;
		}

		_query.Expressions ??= [];

		var addButton = new Button
		{
			Text = "Add Expression",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		addButton.Pressed += OnAddExpressionPressed;
		_contentContainer.AddChild(CreateLabeledRow("Exprs:", addButton));

		if (_query.Expressions.Count == 0)
		{
			_contentContainer.AddChild(CreateIndentedRow(new Label { Text = "(none)" }));
			return;
		}

		for (int i = 0; i < _query.Expressions.Count; i++)
		{
			ForgeQueryExpression expression = _query.Expressions[i] ?? CreateDefaultQueryExpression();
			_query.Expressions[i] = expression;

			var itemRoot = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			headerRow.AddChild(new Label
			{
				Text = $"Item {i + 1}:",
				CustomMinimumSize = new Vector2(LabelWidth, 0),
				HorizontalAlignment = HorizontalAlignment.Right,
			});

			var removeButton = new Button { Text = "Remove" };
			int index = i;
			removeButton.Pressed += () => OnRemoveExpressionPressed(index);
			headerRow.AddChild(removeButton);
			itemRoot.AddChild(headerRow);

			var nestedEditor = new QueryExpressionEditorControl();
			nestedEditor.Setup(expression, NotifyChanged);
			nestedEditor.LayoutChanged += RaiseLayoutChanged;
			itemRoot.AddChild(CreateIndentedRow(nestedEditor));

			_contentContainer.AddChild(itemRoot);
		}
	}

	private void BuildTagContainerEditor()
	{
		if (_query is null || _contentContainer is null)
		{
			return;
		}

		_query.TagContainer ??= new ForgeTagContainer();

		var tagContainerEditor = new TagContainerSelectionControl();
		tagContainerEditor.SetValue(_query.TagContainer.ContainerTags);
		tagContainerEditor.ValueChanged += OnTagContainerChanged;
		_contentContainer.AddChild(CreateLabeledRow("Tags:", tagContainerEditor));
	}

	private void OnExpressionTypeChanged(long index)
	{
		if (_query is null)
		{
			return;
		}

		_query.ExpressionType = (TagQueryExpressionType)(int)index;
		EnsureDefaultData(_query);
		RefreshUi();
		NotifyChanged();
	}

	private void OnAddExpressionPressed()
	{
		if (_query is null)
		{
			return;
		}

		_query.Expressions ??= [];
		_query.Expressions.Add(CreateDefaultQueryExpression());
		RefreshUi();
		NotifyChanged();
	}

	private void OnRemoveExpressionPressed(int index)
	{
		if (_query?.Expressions is null || index < 0 || index >= _query.Expressions.Count)
		{
			return;
		}

		_query.Expressions.RemoveAt(index);
		RefreshUi();
		NotifyChanged();
	}

	private void OnTagContainerChanged(Array<string> tags)
	{
		if (_query is null)
		{
			return;
		}

		_query.TagContainer ??= new ForgeTagContainer();
		_query.TagContainer.ContainerTags = tags;
		NotifyChanged();
	}

	private void NotifyChanged()
	{
		_query?.EmitChanged();
		_onChanged?.Invoke();
		RaiseLayoutChanged();
	}

	private void RaiseLayoutChanged()
	{
		LayoutChanged?.Invoke();
	}
}
#endif
