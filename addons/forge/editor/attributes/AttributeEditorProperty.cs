// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeEditorProperty : EditorProperty, ISerializationListener
{
	private const int ButtonSize = 26;
	private const int PopupSize = 300;

	private Label? _label;
	private Button? _button;
	private Popup? _popup;
	private Tree? _tree;

	public override void _Ready()
	{
		Texture2D dropdownIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("GuiDropdown", "EditorIcons");

		var hBox = new HBoxContainer();
		_label = new Label { Text = "None", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_button = new Button { Icon = dropdownIcon, CustomMinimumSize = new Vector2(ButtonSize, 0) };

		hBox.AddChild(_label);
		hBox.AddChild(_button);
		AddChild(hBox);

		_popup = new Popup { Size = new Vector2I(PopupSize, PopupSize) };
		_tree = new Tree
		{
			HideRoot = true,
			AnchorRight = 1,
			AnchorBottom = 1,
		};
		_popup.AddChild(_tree);

		var bg = new StyleBoxFlat
		{
			BgColor = EditorInterface.Singleton
				.GetEditorTheme()
				.GetColor("dark_color_2", "Editor"),
		};
		_tree.AddThemeStyleboxOverride("panel", bg);

		AddChild(_popup);

		BuildAttributeTree(_tree);

		_button.Pressed += OnButtonPressed;
		_tree.ItemActivated += OnTreeItemActivated;
	}

	public override void _UpdateProperty()
	{
		if (_label is null || !IsInstanceValid(_label))
		{
			return;
		}

		var value = GetEditedObject().Get(GetEditedProperty()).AsString();
		_label.Text = string.IsNullOrEmpty(value) ? "None" : value;
	}

	public override void _ExitTree()
	{
		ReleaseUiState();
		FreeAllChildren();
		base._ExitTree();
	}

	public void OnBeforeSerialize()
	{
		ReleaseUiState();
		FreeAllChildren();
	}

	public void OnAfterDeserialize()
	{
		// This method was intentionally left blank.
	}

	private static void BuildAttributeTree(Tree tree)
	{
		TreeItem root = tree.CreateItem();

		foreach (var attributeSet in EditorUtils.GetAttributeSetOptions())
		{
			TreeItem setItem = tree.CreateItem(root);
			setItem.SetText(0, attributeSet);
			setItem.Collapsed = true;

			foreach (var attribute in EditorUtils.GetAttributeOptions(attributeSet))
			{
				TreeItem attributeItem = tree.CreateItem(setItem);
				var attributePath = $"{attributeSet}.{attribute}";
				attributeItem.SetText(0, attribute);
				attributeItem.SetMeta("attribute_path", attributePath);
			}
		}
	}

	private void OnButtonPressed()
	{
		if (_button is null || _popup is null || !IsInstanceValid(_button) || !IsInstanceValid(_popup))
		{
			return;
		}

		Window win = GetWindow();
		_popup.Position = (Vector2I)_button.GlobalPosition
			+ win.Position
			- new Vector2I(PopupSize - ButtonSize, -30);
		_popup.Popup();
	}

	private void OnTreeItemActivated()
	{
		if (_tree is null || _popup is null || _label is null
			|| !IsInstanceValid(_tree) || !IsInstanceValid(_popup) || !IsInstanceValid(_label))
		{
			return;
		}

		TreeItem item = _tree.GetSelected();
		if (item?.HasMeta("attribute_path") != true)
		{
			return;
		}

		var fullPath = item.GetMeta("attribute_path").AsString();
		_label.Text = fullPath;
		EmitChanged(GetEditedProperty(), fullPath);
		_popup.Hide();
	}

	private void ReleaseUiState()
	{
		if (_button is not null && IsInstanceValid(_button))
		{
			_button.Pressed -= OnButtonPressed;
		}

		if (_tree is not null && IsInstanceValid(_tree))
		{
			_tree.ItemActivated -= OnTreeItemActivated;
		}

		_label = null;
		_button = null;
		_popup = null;
		_tree = null;
	}

	private void FreeAllChildren()
	{
		for (var i = GetChildCount() - 1; i >= 0; i--)
		{
			Node child = GetChild(i);
			RemoveChild(child);
			child.Free();
		}
	}
}
#endif
