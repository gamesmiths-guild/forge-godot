// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class EntityView : VBoxContainer
{
	[Export]
	public ForgeEntity? Entity { get; set; }

	public override void _Ready()
	{
		base._Ready();

		Debug.Assert(Entity is not null, $"{nameof(Entity)} reference is missing.");

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
