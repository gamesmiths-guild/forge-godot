// Copyright © 2025 Gamesmiths Guild.

using System;
using System.Linq;
using System.Reflection;
using Godot;

using ForgeAttributeSet = Gamesmiths.Forge.Core.AttributeSet;

namespace Gamesmiths.Forge.Core.Godot;

[Tool]
public partial class AttributeSetEditor : VBoxContainer
{
	private OptionButton _attributeSetClassOptionButton;

	[Export]
	public PackedScene AttributeScene { get; set; }

	public AttributeSet TargetAttributeSet { get; set; }

	public bool IsPluginInstance { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (!IsPluginInstance)
		{
			return;
		}

		AttributeScene = ResourceLoader.Load<PackedScene>("res://addons/forge/core/Attribute.tscn");

		_attributeSetClassOptionButton = GetNode<OptionButton>("%OptionButton");
		_attributeSetClassOptionButton.Clear();

		// Add a default empty option if needed
		_attributeSetClassOptionButton.AddItem("Select AttributeSet Class");

		foreach (var option in GetAttributeSetOptions())
		{
			_attributeSetClassOptionButton.AddItem(option);
		}

		_attributeSetClassOptionButton.ItemSelected += AttributeSetClassOptionButton_ItemSelected;

		// Set initial selection if TargetAttributeSet already has a value.
		if (TargetAttributeSet is not null && !string.IsNullOrEmpty(TargetAttributeSet.AttributeSetClass))
		{
			// Search for the item that matches the current value
			for (var i = 0; i < _attributeSetClassOptionButton.GetItemCount(); i++)
			{
				if (_attributeSetClassOptionButton.GetItemText(i) == TargetAttributeSet.AttributeSetClass)
				{
					_attributeSetClassOptionButton.Selected = i;
					AttributeSetClassOptionButton_ItemSelected(i);
					break;
				}
			}
		}
		else if (_attributeSetClassOptionButton.GetItemCount() > 1)
		{
			// If no value is set, select the first real option (skipping the default)
			_attributeSetClassOptionButton.Selected = 1;
			AttributeSetClassOptionButton_ItemSelected(1);
		}
	}

	/// <summary>
	/// Uses reflection to gather all classes inheriting from AttributeSet and their fields of type Attribute.
	/// </summary>
	/// <returns>An array with the avaiable attributes.</returns>
	private static string[] GetAttributeSetOptions()
	{
		var options = new System.Collections.Generic.List<string>();

		// Get all types in the current assembly
		Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		// Find all types that subclass AttributeSet
		foreach (Type attributeSetType in allTypes.Where(x => x.IsSubclassOf(typeof(ForgeAttributeSet))))
		{
			options.Add(attributeSetType.Name);
		}

		return [.. options];
	}

	private void AttributeSetClassOptionButton_ItemSelected(long index)
	{
		if (TargetAttributeSet.AttributeSetClass != _attributeSetClassOptionButton.GetItemText((int)index))
		{
			TargetAttributeSet.AttributeSetClass = _attributeSetClassOptionButton.GetItemText((int)index);
			TargetAttributeSet.InitialAttributeValues.Clear();

			foreach (Node child in GetTree().GetNodesInGroup("attributes"))
			{
				RemoveChild(child);
			}
		}

		Type targetType = Array.Find(
			Assembly.GetExecutingAssembly().GetTypes(),
			x => x.Name == _attributeSetClassOptionButton.GetItemText((int)index));

		if (targetType is null)
		{
			return;
		}

		System.Collections.Generic.IEnumerable<PropertyInfo> attributeProperties = targetType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.PropertyType == typeof(Attribute));

		ForgeAttributeSet attributeSet = TargetAttributeSet.GetAttributeSet();

		foreach (var attributeName in attributeProperties.Select(x => x.Name))
		{
			var attributeScene = (VBoxContainer)AttributeScene.Instantiate();
			AddChild(attributeScene);
			attributeScene.AddToGroup("attributes");

			SpinBox spinBoxCurrent = attributeScene.GetNode<SpinBox>("%Current");
			SpinBox spinBoxMin = attributeScene.GetNode<SpinBox>("%Min");
			SpinBox spinBoxMax = attributeScene.GetNode<SpinBox>("%Max");

			if (TargetAttributeSet.InitialAttributeValues is not null)
			{
				if (!TargetAttributeSet.InitialAttributeValues.TryGetValue(attributeName, out AttributeValues value))
				{
					value = new AttributeValues(
						attributeSet.AttributesMap[$"{TargetAttributeSet.AttributeSetClass}.{attributeName}"].CurrentValue,
						attributeSet.AttributesMap[$"{TargetAttributeSet.AttributeSetClass}.{attributeName}"].Min,
						attributeSet.AttributesMap[$"{TargetAttributeSet.AttributeSetClass}.{attributeName}"].Max);
					TargetAttributeSet.InitialAttributeValues.Add(attributeName, value);
				}

				attributeScene.GetNode<Label>("%Attribute").Text = $" {attributeName}";

				spinBoxCurrent.Value = value.Current;
				spinBoxMin.Value = value.Min;
				spinBoxMax.Value = value.Max;

				spinBoxCurrent.MinValue = value.Min;
				spinBoxCurrent.MaxValue = value.Max;
				spinBoxMin.MaxValue = value.Max;
				spinBoxMax.MinValue = value.Min;

				spinBoxCurrent.ValueChanged += (double newValue) =>
				{
					if (TargetAttributeSet is not null)
					{
						TargetAttributeSet.InitialAttributeValues[attributeName] = new AttributeValues(
							(int)newValue, (int)spinBoxMin.Value, (int)spinBoxMax.Value);

						TargetAttributeSet.NotifyPropertyListChanged();
					}
				};

				spinBoxMin.ValueChanged += (double newValue) =>
				{
					if (TargetAttributeSet is not null)
					{
						// Update dynamic limits.
						spinBoxCurrent.MinValue = newValue;
						spinBoxMax.MinValue = newValue;

						TargetAttributeSet.InitialAttributeValues[attributeName] = new AttributeValues(
							(int)spinBoxCurrent.Value, (int)newValue, (int)spinBoxMax.Value);

						TargetAttributeSet.NotifyPropertyListChanged();
					}
				};

				spinBoxMax.ValueChanged += (double newValue) =>
				{
					if (TargetAttributeSet is not null)
					{
						// Update dynamic limits.
						spinBoxCurrent.MaxValue = newValue;
						spinBoxMin.MaxValue = newValue;

						TargetAttributeSet.InitialAttributeValues[attributeName] = new AttributeValues(
							(int)spinBoxCurrent.Value, (int)spinBoxMin.Value, (int)newValue);

						TargetAttributeSet.NotifyPropertyListChanged();
					}
				};
			}
		}
	}
}
