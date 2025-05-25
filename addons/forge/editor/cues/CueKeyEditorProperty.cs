// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Cues;

[Tool]
public partial class CueKeyEditorProperty : EditorProperty
{
	private readonly OptionButton _dropdown = new();

	public override void _Ready()
	{
		AddChild(_dropdown);
		AddFocusable(_dropdown);

		foreach (var cue in ForgeContext.RegisteredCues ?? [])
		{
			_dropdown.AddItem(cue);
		}

		_dropdown.ItemSelected += Dropdown_ItemSelected;
	}

	public override void _UpdateProperty()
	{
		Variant value = GetEditedObject().Get(GetEditedProperty());
		var cueKey = value.AsString();

		for (var i = 0; i < _dropdown.ItemCount; i++)
		{
			if (_dropdown.GetItemText(i) == cueKey)
			{
				_dropdown.Select(i);
				return;
			}
		}

		_dropdown.Select(0); // fallback
	}

	private void Dropdown_ItemSelected(long index)
	{
		EmitChanged(GetEditedProperty(), _dropdown.GetItemText((int)index));
		NotifyPropertyListChanged();
	}
}
#endif
