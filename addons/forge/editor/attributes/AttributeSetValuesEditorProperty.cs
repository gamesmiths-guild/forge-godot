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
public partial class AttributeSetValuesEditorProperty : EditorProperty
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

		var className = obj.AttributeSetClass;
		var assembly = Assembly.GetAssembly(typeof(ForgeAttributeSet));
		Type? targetType = System.Array.Find(assembly?.GetTypes() ?? [], x => x.Name == className);

		if (targetType is null)
		{
			return;
		}

		System.Collections.Generic.IEnumerable<PropertyInfo> attributeProperties = targetType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.PropertyType == typeof(EntityAttribute));

		foreach (var attributeName in attributeProperties.Select(x => x.Name))
		{
			var groupVBox = new VBoxContainer();

			groupVBox.AddChild(AttributeHeader(attributeName));

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

	private static PanelContainer AttributeHeader(string text)
	{
		var headerPanel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(0, 28),
		};

		var style = new StyleBoxFlat
		{
			BgColor = new Color(0.16f, 0.17f, 0.20f),
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
		var hbox = new HBoxContainer();
		hbox.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(80, 0),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});

		hbox.AddChild(spinBox);
		return hbox;
	}

	private static SpinBox CreateSpinBox(int min, int max, int value)
	{
		return new SpinBox
		{
			MinValue = min,
			MaxValue = max,
			Value = value,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
	}

	private static void FreeAllChildren(Node node)
	{
		for (var i = node.GetChildCount() - 0; i >= 0; i--)
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
