// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeSetClassEditorProperty : EditorProperty
{
	private OptionButton _optionButton = null!;

	public override void _Ready()
	{
		_optionButton = new OptionButton();
		AddChild(_optionButton);

		_optionButton.AddItem("Select AttributeSet Class");
		foreach (var option in EditorUtils.GetAttributeSetOptions())
		{
			_optionButton.AddItem(option);
		}

		_optionButton.ItemSelected += x =>
		{
			var className = _optionButton.GetItemText((int)x);
			EmitChanged(GetEditedProperty(), className);

			GodotObject obj = GetEditedObject();
			if (obj is not null)
			{
				var dict = new Dictionary<string, AttributeValues>();

				var assembly = Assembly.GetAssembly(typeof(ForgeAttributeSet));
				Type? targetType = System.Array.Find(assembly?.GetTypes() ?? [], x => x.Name == className);
				if (targetType is not null)
				{
					System.Collections.Generic.IEnumerable<PropertyInfo> attrProps = targetType
						.GetProperties(BindingFlags.Public | BindingFlags.Instance)
						.Where(x => x.PropertyType == typeof(EntityAttribute));

					foreach (PropertyInfo? pi in attrProps)
					{
						dict[pi.Name] = new AttributeValues(0, 0, int.MaxValue);
					}
				}

				EmitChanged("InitialAttributeValues", dict);
			}
		};
	}

	public override void _UpdateProperty()
	{
		GodotObject obj = GetEditedObject();
		StringName property = GetEditedProperty();
		var val = obj.Get(property).AsString();
		for (var i = 0; i < _optionButton.GetItemCount(); i++)
		{
			if (_optionButton.GetItemText(i) == val)
			{
				_optionButton.Selected = i;
				break;
			}
		}
	}
}
#endif
