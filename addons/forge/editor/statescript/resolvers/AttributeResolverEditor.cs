// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads a value from a Forge entity attribute. Shows attribute set and attribute dropdowns.
/// </summary>
[Tool]
internal sealed partial class AttributeResolverEditor : NodeEditorProperty
{
	private OptionButton? _setDropdown;
	private OptionButton? _attributeDropdown;
	private string _selectedSetClass = string.Empty;
	private string _selectedAttribute = string.Empty;

	/// <inheritdoc/>
	public override string DisplayName => "Attribute";

	/// <inheritdoc/>
	public override Type ValueType => typeof(Variant128);

	/// <inheritdoc/>
	public override string ResolverTypeId => "Attribute";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		// Attributes produce numeric values — compatible with numeric types and Variant128.
		return expectedType == typeof(Variant128)
			|| expectedType == typeof(int)
			|| expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(long);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (property?.Resolver is AttributeResolverResource attrRes)
		{
			_selectedSetClass = attrRes.AttributeSetClass;
			_selectedAttribute = attrRes.AttributeName;
		}

		var setRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(setRow);

		setRow.AddChild(new Label
		{
			Text = "Set:",
			CustomMinimumSize = new Vector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_setDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateSetDropdown();
		setRow.AddChild(_setDropdown);

		var attrRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(attrRow);

		attrRow.AddChild(new Label
		{
			Text = "Attr:",
			CustomMinimumSize = new Vector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_attributeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateAttributeDropdown();
		attrRow.AddChild(_attributeDropdown);

		_setDropdown.ItemSelected += _ =>
		{
			if (_setDropdown is null)
			{
				return;
			}

			_selectedSetClass = _setDropdown.GetItemText(_setDropdown.Selected);
			_selectedAttribute = string.Empty;
			PopulateAttributeDropdown();
			onChanged();
		};

		_attributeDropdown.ItemSelected += _ =>
		{
			if (_attributeDropdown is null)
			{
				return;
			}

			_selectedAttribute = _attributeDropdown.GetItemText(_attributeDropdown.Selected);
			onChanged();
		};
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AttributeResolverResource
		{
			AttributeSetClass = _selectedSetClass,
			AttributeName = _selectedAttribute,
		};
	}

	private void PopulateSetDropdown()
	{
		if (_setDropdown is null)
		{
			return;
		}

		_setDropdown.Clear();

		foreach (var option in EditorUtils.GetAttributeSetOptions())
		{
			_setDropdown.AddItem(option);
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedSetClass))
		{
			for (var i = 0; i < _setDropdown.GetItemCount(); i++)
			{
				if (_setDropdown.GetItemText(i) == _selectedSetClass)
				{
					_setDropdown.Selected = i;
					return;
				}
			}
		}

		// Default to first if available.
		if (_setDropdown.GetItemCount() > 0)
		{
			_setDropdown.Selected = 0;
			_selectedSetClass = _setDropdown.GetItemText(0);
		}
	}

	private void PopulateAttributeDropdown()
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_attributeDropdown.Clear();

		foreach (var option in EditorUtils.GetAttributeOptions(_selectedSetClass))
		{
			_attributeDropdown.AddItem(option);
		}

		// Restore selection.
		if (!string.IsNullOrEmpty(_selectedAttribute))
		{
			for (var i = 0; i < _attributeDropdown.GetItemCount(); i++)
			{
				if (_attributeDropdown.GetItemText(i) == _selectedAttribute)
				{
					_attributeDropdown.Selected = i;
					return;
				}
			}
		}

		// Default to first if available.
		if (_attributeDropdown.GetItemCount() > 0)
		{
			_attributeDropdown.Selected = 0;
			_selectedAttribute = _attributeDropdown.GetItemText(0);
		}
	}
}
#endif
