// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Core.Godot;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class EntityView : VBoxContainer
{
	[Export]
	public ForgeEntity Entity { get; set; }

	public override void _Ready()
	{
		base._Ready();

		foreach (AttributeInfo attributeInfo in Entity.Attributes.WithKeys())
		{
			var newLabel = new Label
			{
				Text = $"{attributeInfo.FullKey}: {attributeInfo.Value.CurrentValue}",
			};

			attributeInfo.Value.OnValueChanged += (_, _) =>
			{
				newLabel.Text = $"{attributeInfo.FullKey}: {attributeInfo.Value.CurrentValue}";
			};

			AddChild(newLabel);
		}
	}
}
