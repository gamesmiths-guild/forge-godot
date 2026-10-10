// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A progress bar that follows one attribute of a <see cref="ForgeEntity"/>.
/// </summary>
/// <remarks>
/// Place it below the entity in the scene tree, so the entity is ready first.
/// </remarks>
public partial class AttributeBar : ProgressBar
{
	private EntityAttribute? _attribute;

	[Export]
	public ForgeEntity? Entity { get; set; }

	[Export]
	public string Attribute { get; set; } = "HeroAttributes.Health";

	public override void _Ready()
	{
		if (Entity is null)
		{
			return;
		}

		_attribute = Entity.Attributes[Attribute];
		_attribute.OnValueChanged += OnAttributeChanged;
		OnAttributeChanged(_attribute, 0);
	}

	public override void _ExitTree()
	{
		if (_attribute is not null)
		{
			_attribute.OnValueChanged -= OnAttributeChanged;
		}
	}

	private void OnAttributeChanged(EntityAttribute attribute, int change)
	{
		MaxValue = attribute.Max;
		Value = attribute.CurrentValue;
	}
}
