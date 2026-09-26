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

	public void OnBeforeSerialize()
	{
		// The children stay: EditorProperty keeps raw pointers to its own containers among them, and an Inspector
		// hidden behind another dock tab keeps using this editor after the reload, until it is shown and rebuilds.
		ReleaseUiState();
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
}
#endif
