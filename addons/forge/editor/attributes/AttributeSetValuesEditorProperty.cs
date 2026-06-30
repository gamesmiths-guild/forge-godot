// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeSetValuesEditorProperty : EditorProperty, ISerializationListener
{
	public override void _Ready()
	{
		var attributesRoot = new VBoxContainer { Name = "AttributesRoot" };
		AddChild(attributesRoot);
		SetBottomEditor(attributesRoot);
	}

	public override void _UpdateProperty()
	{
		VBoxContainer attributesRoot = GetNodeOrNull<VBoxContainer>("AttributesRoot");

		if (attributesRoot is null)
		{
			return;
		}

		FreeAllChildren(attributesRoot);

		if (GetEditedObject() is not ForgeAttributeSet obj
			|| string.IsNullOrEmpty(obj.AttributeSetClass)
			|| obj.InitialAttributeValues is null)
		{
			return;
		}

		string className = obj.AttributeSetClass;
		var assembly = Assembly.GetAssembly(typeof(ForgeAttributeSet));
		Type? targetType = System.Array.Find(assembly?.GetTypes() ?? [], x => x.Name == className);

		if (targetType is null)
		{
			return;
		}

		System.Collections.Generic.IEnumerable<PropertyInfo> attributeProperties = targetType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.PropertyType == typeof(EntityAttribute));

		AttributeSet? attributeSetInstance = obj.GetAttributeSet();

		foreach (PropertyInfo property in attributeProperties)
		{
			string attributeName = property.Name;

			var groupVBox = new VBoxContainer();

			groupVBox.AddChild(AttributeHeader(GetHeaderText(property, attributeSetInstance)));

			AttributeValues value = obj.InitialAttributeValues.TryGetValue(attributeName, out AttributeValues? v)
				? v
				: new AttributeValues(0, 0, int.MaxValue);

			SpinBox spinDefault = CreateSpinBox(value.Min, value.Max, value.Default);
			SpinBox spinMin = CreateSpinBox(int.MinValue, value.Max, value.Min);
			SpinBox spinMax = CreateSpinBox(value.Min, int.MaxValue, value.Max);

			groupVBox.AddChild(AttributeFieldRow("Default", spinDefault));
			groupVBox.AddChild(AttributeFieldRow("Min", spinMin));
			groupVBox.AddChild(AttributeFieldRow("Max", spinMax));

			spinDefault.ValueChanged += x =>
			{
				UpdateAndEmit(obj, attributeName, (int)x, (int)spinMin.Value, (int)spinMax.Value);
			};

			spinMin.ValueChanged += x =>
			{
				spinDefault.MinValue = x;
				spinMax.MinValue = x;
				UpdateAndEmit(obj, attributeName, (int)spinDefault.Value, (int)x, (int)spinMax.Value);
			};

			spinMax.ValueChanged += x =>
			{
				spinDefault.MaxValue = x;
				spinMin.MaxValue = x;
				UpdateAndEmit(obj, attributeName, (int)spinDefault.Value, (int)spinMin.Value, (int)x);
			};

			attributesRoot.AddChild(groupVBox);
		}
	}

	public void OnBeforeSerialize()
	{
		VBoxContainer? attributesRoot = GetNodeOrNull<VBoxContainer>("AttributesRoot");
		if (attributesRoot is not null)
		{
			for (int i = attributesRoot.GetChildCount() - 1; i >= 0; i--)
			{
				Node child = attributesRoot.GetChild(i);
				attributesRoot.RemoveChild(child);
				child.Free();
			}
		}
	}

	public void OnAfterDeserialize()
	{
	}

	private static string GetHeaderText(PropertyInfo property, AttributeSet? attributeSetInstance)
	{
		string propertyName = property.Name;

		if (attributeSetInstance is null
			|| property.GetValue(attributeSetInstance) is not EntityAttribute attribute)
		{
			return propertyName;
		}

		string key = attribute.Key.ToString();
		string prefix = $"{attributeSetInstance.GetType().Name}.";
		string registeredName = key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			? key[prefix.Length..]
			: key;

		return string.Equals(propertyName, registeredName, StringComparison.OrdinalIgnoreCase)
			? propertyName
			: $"{propertyName}  ({registeredName})";
	}

	private static PanelContainer AttributeHeader(string text)
	{
		var headerPanel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(0, 28),
		};

		var style = new StyleBoxFlat
		{
			BgColor = EditorInterface.Singleton.GetEditorTheme().GetColor("dark_color_2", "Editor"),
		};

		headerPanel.AddThemeStyleboxOverride("panel", style);

		var label = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = (SizeFlags)(int)SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 22),
			AutowrapMode = TextServer.AutowrapMode.Off,
		};

		headerPanel.AddChild(label);
		return headerPanel;
	}

	private static HBoxContainer AttributeFieldRow(string label, SpinBox spinBox)
	{
		var hBox = new HBoxContainer();

		hBox.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(80, 0),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});

		hBox.AddChild(spinBox);
		return hBox;
	}

	private static SpinBox CreateSpinBox(int min, int max, int value)
	{
		return new SpinBox
		{
			MinValue = min,
			MaxValue = max,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SelectAllOnFocus = true,
			Value = value,
		};
	}

	private static void FreeAllChildren(Node node)
	{
		for (int i = node.GetChildCount() - 1; i >= 0; i--)
		{
			node.GetChild(i).QueueFree();
		}
	}

	private void UpdateAndEmit(ForgeAttributeSet obj, string name, int def, int min, int max)
	{
		Debug.Assert(obj.InitialAttributeValues is not null, "InitialAttributeValues should not be null here.");

		var dict = new Dictionary<string, AttributeValues>(obj.InitialAttributeValues)
		{
			[name] = new AttributeValues(def, min, max),
		};

		EmitChanged(nameof(ForgeAttributeSet.InitialAttributeValues), dict);
	}
}
#endif
