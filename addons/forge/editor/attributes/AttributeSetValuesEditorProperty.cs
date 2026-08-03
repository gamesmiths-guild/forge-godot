// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeSetValuesEditorProperty : EditorProperty, ISerializationListener
{
	private const int ReadingLabelWidth = 90;
	private const float ReadingLabelAlpha = 0.6f;

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

			EntityAttribute? attribute = attributeSetInstance is null
				? null
				: property.GetValue(attributeSetInstance) as EntityAttribute;

			int decimalPlaces = attribute?.DecimalPlaces ?? 0;

			groupVBox.AddChild(AttributeHeader(GetHeaderText(property, attributeSetInstance, decimalPlaces)));

			AttributeValues value = obj.InitialAttributeValues.TryGetValue(attributeName, out AttributeValues? v)
				? v
				: new AttributeValues(0, 0, int.MaxValue);

			SpinBox spinDefault = CreateSpinBox(value.Min, value.Max, value.Default);
			SpinBox spinMin = CreateSpinBox(int.MinValue, value.Max, value.Min);
			SpinBox spinMax = CreateSpinBox(value.Min, int.MaxValue, value.Max);

			Label readingDefault = CreateReadingLabel(value.Default, decimalPlaces);
			Label readingMin = CreateReadingLabel(value.Min, decimalPlaces);
			Label readingMax = CreateReadingLabel(value.Max, decimalPlaces);

			groupVBox.AddChild(AttributeFieldRow("Default", spinDefault, readingDefault));
			groupVBox.AddChild(AttributeFieldRow("Min", spinMin, readingMin));
			groupVBox.AddChild(AttributeFieldRow("Max", spinMax, readingMax));

			spinDefault.ValueChanged += x =>
			{
				UpdateReading(readingDefault, x, decimalPlaces);
				UpdateAndEmit(obj, attributeName, (int)x, (int)spinMin.Value, (int)spinMax.Value);
			};

			spinMin.ValueChanged += x =>
			{
				spinDefault.MinValue = x;
				spinMax.MinValue = x;
				UpdateReading(readingMin, x, decimalPlaces);
				UpdateAndEmit(obj, attributeName, (int)spinDefault.Value, (int)x, (int)spinMax.Value);
			};

			spinMax.ValueChanged += x =>
			{
				spinDefault.MaxValue = x;
				spinMin.MaxValue = x;
				UpdateReading(readingMax, x, decimalPlaces);
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

	private static string GetHeaderText(PropertyInfo property, AttributeSet? attributeSetInstance, int decimalPlaces)
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

		string name = string.Equals(propertyName, registeredName, StringComparison.OrdinalIgnoreCase)
			? propertyName
			: $"{propertyName}  ({registeredName})";

		if (decimalPlaces <= 0)
		{
			return name;
		}

		return $"{name} — {decimalPlaces} decimal{(decimalPlaces == 1 ? string.Empty : "s")}";
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

	private static HBoxContainer AttributeFieldRow(string label, SpinBox spinBox, Label readingLabel)
	{
		var hBox = new HBoxContainer();

		hBox.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(80, 0),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});

		hBox.AddChild(readingLabel);
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

	private static Label CreateReadingLabel(int rawValue, int decimalPlaces)
	{
		return new Label
		{
			Text = ReadingText(rawValue, decimalPlaces),
			CustomMinimumSize = new Vector2(ReadingLabelWidth, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off,

			Modulate = new Color(1, 1, 1, ReadingLabelAlpha),
		};
	}

	private static void UpdateReading(Label readingLabel, double rawValue, int decimalPlaces)
	{
		if (IsInstanceValid(readingLabel))
		{
			readingLabel.Text = ReadingText((int)rawValue, decimalPlaces);
		}
	}

	private static string ReadingText(int rawValue, int decimalPlaces)
	{
		if (decimalPlaces <= 0)
		{
			return string.Empty;
		}

		// Invariant so the separator matches what the SpinBox beside it renders, whatever the editor's locale.
		return $"({Quantization.ToDisplayString(rawValue, decimalPlaces, CultureInfo.InvariantCulture)})";
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
