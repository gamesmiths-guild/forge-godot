// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Cues;

[Tool]
public partial class CueKeyEditorProperty : EditorProperty, ISerializationListener
{
	private const int ButtonSize = 26;
	private const int PopupSize = 300;

	private Label? _label;
	private Button? _button;
	private Popup? _popup;
	private Tree? _tree;

	public override void _Ready()
	{
		Texture2D dropdownIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiDropdown", "EditorIcons");

		var hbox = new HBoxContainer();
		_label = new Label { Text = "None", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_button = new Button { Icon = dropdownIcon, CustomMinimumSize = new Vector2(ButtonSize, 0) };

		hbox.AddChild(_label);
		hbox.AddChild(_button);
		AddChild(hbox);

		_popup = new Popup { Size = new Vector2I(PopupSize, PopupSize) };
		_tree = new Tree
		{
			HideRoot = true,
			AnchorRight = 1,
			AnchorBottom = 1,
		};
		_popup.AddChild(_tree);

		var backgroundStyle = new StyleBoxFlat
		{
			BgColor = EditorInterface.Singleton.GetEditorTheme().GetColor("base_color", "Editor"),
		};
		_tree.AddThemeStyleboxOverride("panel", backgroundStyle);

		AddChild(_popup);

		ForgeData pluginData = ResourceLoader.Load<ForgeData>(ForgeData.ForgeDataResourcePath);
		var tagsManager = new TagsManager([.. pluginData.RegisteredTags]);
		TreeItem root = _tree.CreateItem();
		BuildTreeRecursively(_tree, root, tagsManager.RootNode);

		_button.Pressed += OnButtonPressed;
		_tree.ItemActivated += OnTreeItemActivated;
	}

	public override void _UpdateProperty()
	{
		string property = GetEditedObject().Get(GetEditedProperty()).AsString();

		if (_label is not null && IsInstanceValid(_label))
		{
			_label.Text = string.IsNullOrEmpty(property) ? "None" : property;
		}
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
	}

	private static void BuildTreeRecursively(Tree tree, TreeItem currentTreeItem, TagNode currentNode)
	{
		foreach (TagNode childTagNode in currentNode.ChildTags)
		{
			TreeItem childTreeNode = tree.CreateItem(currentTreeItem);
			childTreeNode.SetText(0, childTagNode.TagKey);
			childTreeNode.Collapsed = true;
			BuildTreeRecursively(tree, childTreeNode, childTagNode);
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
		if (item is null)
		{
			return;
		}

		var segments = new List<string>();
		TreeItem current = item;
		while (current.GetParent() is not null)
		{
			segments.Insert(0, current.GetText(0));
			current = current.GetParent();
		}

		string fullPath = string.Join(".", segments);

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
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			Node child = GetChild(i);
			RemoveChild(child);
			child.Free();
		}
	}
}
#endif
