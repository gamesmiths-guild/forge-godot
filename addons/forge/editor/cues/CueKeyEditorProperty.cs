// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Cues;

[Tool]
public partial class CueKeyEditorProperty : EditorProperty
{
	private const int ButtonSize = 26;
	private const int PopupSize = 300;

	private Label _label = null!;

	public override void _Ready()
	{
		Texture2D dropdownIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiDropdown", "EditorIcons");

		var hbox = new HBoxContainer();
		_label = new Label { Text = "None", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var button = new Button { Icon = dropdownIcon, CustomMinimumSize = new Vector2(ButtonSize, 0) };

		hbox.AddChild(_label);
		hbox.AddChild(button);
		AddChild(hbox);

		var popup = new Popup { Size = new Vector2I(PopupSize, PopupSize) };
		var tree = new Tree
		{
			HideRoot = true,
			AnchorRight = 1,
			AnchorBottom = 1,
		};
		popup.AddChild(tree);

		var backgroundStyle = new StyleBoxFlat
		{
			BgColor = EditorInterface.Singleton.GetEditorTheme().GetColor("base_color", "Editor"),
		};
		tree.AddThemeStyleboxOverride("panel", backgroundStyle);

		AddChild(popup);

		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");
		var tagsManager = new TagsManager([.. pluginData.RegisteredTags]);
		TreeItem root = tree.CreateItem();
		BuildTreeRecursively(tree, root, tagsManager.RootNode);

		button.Pressed += () =>
		{
			Window win = GetWindow();
			popup.Position = (Vector2I)button.GlobalPosition
				+ win.Position
				- new Vector2I(PopupSize - ButtonSize, -30);
			popup.Popup();
		};

		tree.ItemActivated += () =>
		{
			TreeItem item = tree.GetSelected();
			if (item is null)
			{
				return;
			}

			// Build full path from root.
			var segments = new List<string>();
			TreeItem current = item;
			while (current.GetParent() is not null)
			{
				segments.Insert(0, current.GetText(0));
				current = current.GetParent();
			}

			var fullPath = string.Join(".", segments);

			_label.Text = fullPath;
			EmitChanged(GetEditedProperty(), fullPath);
			popup.Hide();
		};
	}

	public override void _UpdateProperty()
	{
		var property = GetEditedObject().Get(GetEditedProperty()).AsString();
		_label.Text = string.IsNullOrEmpty(property) ? "None" : property;
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
}
#endif
