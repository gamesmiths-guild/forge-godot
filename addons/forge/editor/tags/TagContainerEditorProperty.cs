// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagContainerEditorProperty : EditorProperty, ISerializationListener
{
	private TagContainerSelectionControl? _editor;

	public override void _Ready()
	{
		_editor = new TagContainerSelectionControl();
		_editor.ValueChanged += OnValueChanged;
		AddChild(_editor);
		SetBottomEditor(_editor);
	}

	public override void _UpdateProperty()
	{
		if (_editor is null || !IsInstanceValid(_editor))
		{
			return;
		}

		GodotObject obj = GetEditedObject();
		string propertyName = GetEditedProperty();
		_editor.SetValue(obj.Get(propertyName).AsGodotArray<string>());
	}

	public override void _ExitTree()
	{
		ReleaseUiState();
		FreeAllChildren();
		base._ExitTree();
	}

	public void OnBeforeSerialize()
	{
	}

	public void OnAfterDeserialize()
	{
	}

	private void OnValueChanged(Array<string> value)
	{
		EmitChanged(GetEditedProperty(), value);
	}

	private void ReleaseUiState()
	{
		if (_editor is not null && IsInstanceValid(_editor))
		{
			_editor.ValueChanged -= OnValueChanged;
		}

		_editor = null;
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
