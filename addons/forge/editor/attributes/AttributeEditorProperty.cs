// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeEditorProperty : EditorProperty
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

		var bg = new StyleBoxFlat
		{
			BgColor = EditorInterface.Singleton
				.GetEditorTheme()
				.GetColor("dark_color_2", "Editor"),
		};
		tree.AddThemeStyleboxOverride("panel", bg);

		AddChild(popup);

		BuildAttributeTree(tree);

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
			if (item?.HasMeta("attribute_path") != true)
			{
				return;
			}

			var fullPath = item.GetMeta("attribute_path").AsString();
			_label.Text = fullPath;
			EmitChanged(GetEditedProperty(), fullPath);
			popup.Hide();
		};
	}

	public override void _UpdateProperty()
	{
		var value = GetEditedObject().Get(GetEditedProperty()).AsString();
		_label.Text = string.IsNullOrEmpty(value) ? "None" : value;
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
}
#endif
