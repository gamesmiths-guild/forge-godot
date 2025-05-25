// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

[Tool]
public partial class AttributeSetEditor : VBoxContainer
{
	private OptionButton? _attributeSetClassOptionButton;

	private PackedScene? _attributeScene;

	public ForgeAttributeSet? TargetAttributeSet { get; set; }

	public bool IsPluginInstance { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (!IsPluginInstance)
		{
			return;
		}

		_attributeScene = ResourceLoader.Load<PackedScene>("uid://csgwfxn45gs4m");

		_attributeSetClassOptionButton = GetNode<OptionButton>("%OptionButton");
		_attributeSetClassOptionButton.Clear();

		// Add a default empty option if needed
		_attributeSetClassOptionButton.AddItem("Select AttributeSet Class");

		foreach (var option in EditorUtils.GetAttributeSetOptions())
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
	}

	private void AttributeSetClassOptionButton_ItemSelected(long index)
	{
		Debug.Assert(
			_attributeSetClassOptionButton is not null,
			$"{nameof(_attributeSetClassOptionButton)} should have been initialized on _Ready().");
		Debug.Assert(
			TargetAttributeSet is not null,
			$"{nameof(TargetAttributeSet)} should have been initialized by the inspector plugin.");
		Debug.Assert(
			TargetAttributeSet.InitialAttributeValues is not null,
			$"{nameof(TargetAttributeSet.InitialAttributeValues)} should have been initialized.");
		Debug.Assert(
			_attributeScene is not null,
			$"{nameof(_attributeScene)} should have been initialized on _Ready().");

		if (TargetAttributeSet.AttributeSetClass != _attributeSetClassOptionButton.GetItemText((int)index))
		{
			TargetAttributeSet.AttributeSetClass = _attributeSetClassOptionButton.GetItemText((int)index);
			TargetAttributeSet.InitialAttributeValues.Clear();

			foreach (Node child in GetTree().GetNodesInGroup("attributes"))
			{
				RemoveChild(child);
			}
		}

		Type? targetType = Array.Find(
			Assembly.GetExecutingAssembly().GetTypes(),
			x => x.Name == _attributeSetClassOptionButton.GetItemText((int)index));

		if (targetType is null)
		{
			return;
		}

		System.Collections.Generic.IEnumerable<PropertyInfo> attributeProperties = targetType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.PropertyType == typeof(EntityAttribute));

		AttributeSet? attributeSet = TargetAttributeSet.GetAttributeSet();

		if (attributeSet is null)
		{
			return;
		}

		foreach (var attributeName in attributeProperties.Select(x => x.Name))
		{
			var attributeScene = (VBoxContainer)_attributeScene.Instantiate();
			AddChild(attributeScene);
			attributeScene.AddToGroup("attributes");

			SpinBox spinBoxCurrent = attributeScene.GetNode<SpinBox>("%Current");
			SpinBox spinBoxMin = attributeScene.GetNode<SpinBox>("%Min");
			SpinBox spinBoxMax = attributeScene.GetNode<SpinBox>("%Max");

			if (TargetAttributeSet.InitialAttributeValues is not null)
			{
				if (!TargetAttributeSet.InitialAttributeValues.TryGetValue(attributeName, out AttributeValues? value))
				{
					var attributeSetClass = TargetAttributeSet.AttributeSetClass;

					value = new AttributeValues(
						attributeSet.AttributesMap[$"{attributeSetClass}.{attributeName}"].CurrentValue,
						attributeSet.AttributesMap[$"{attributeSetClass}.{attributeName}"].Min,
						attributeSet.AttributesMap[$"{attributeSetClass}.{attributeName}"].Max);
					TargetAttributeSet.InitialAttributeValues.Add(attributeName, value);
				}

				attributeScene.GetNode<Label>("%Attribute").Text = $" {attributeName}";

				spinBoxCurrent.Value = value.Default;
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
#endif
