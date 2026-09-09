// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

public partial class Hub : Control
{
	private static readonly Color UnselectedCardColor = new(0.78f, 0.81f, 0.85f);

	private readonly List<Button> _cards = [];

	private int _selectedIndex = -1;

	[Export]
	public Array<DemoEntry> Demos { get; set; } = [];

	[Export]
	public required VBoxContainer CardList { get; set; }

	[Export]
	public required Label TitleLabel { get; set; }

	[Export]
	public required Label TaglineLabel { get; set; }

	[Export]
	public required RichTextLabel BlurbLabel { get; set; }

	[Export]
	public required RichTextLabel HighlightsLabel { get; set; }

	[Export]
	public required Button PlayButton { get; set; }

	public override void _Ready()
	{
		base._Ready();

		PlayButton.Pressed += OnPlayPressed;

		for (int i = 0; i < Demos.Count; i++)
		{
			int index = i;

			var card = new Button
			{
				Text = Demos[i].Title,
				Alignment = HorizontalAlignment.Left,
				CustomMinimumSize = new Vector2(0, 36),
			};

			card.Pressed += () => Select(index);

			CardList.AddChild(card);
			_cards.Add(card);
		}

		if (Demos.Count > 0)
		{
			Select(0);
		}
	}

	private void Select(int index)
	{
		_selectedIndex = index;

		DemoEntry demo = Demos[index];

		TitleLabel.Text = demo.Title;
		TitleLabel.AddThemeColorOverride("font_color", demo.Accent);
		TaglineLabel.Text = demo.Tagline;
		BlurbLabel.Text = demo.Blurb;

		string bullet = $"[color=#{demo.Accent.ToHtml(false)}]•[/color]  ";
		var highlights = new StringBuilder();

		foreach (string highlight in demo.Highlights)
		{
			highlights.Append(bullet).Append(highlight).Append('\n');
		}

		HighlightsLabel.Text = highlights.ToString();

		for (int i = 0; i < _cards.Count; i++)
		{
			_cards[i].AddThemeColorOverride("font_color", i == index ? demo.Accent : UnselectedCardColor);
		}

		PlayButton.Disabled = demo.Scene is null;
	}

	private void OnPlayPressed()
	{
		if (_selectedIndex < 0 || Demos[_selectedIndex].Scene is null)
		{
			return;
		}

		GetTree().Root.GetNode<Main>("Main").ChangeScene(Demos[_selectedIndex].Scene!);
	}
}
