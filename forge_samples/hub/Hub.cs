// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

public partial class Hub : Control
{
	private static readonly Color _unselectedCardColor = new(0.78f, 0.81f, 0.85f);

	private static readonly StringName _fontColor = "font_color";

	private readonly List<DemoEntry> _entries = [];

	private readonly List<Button> _cards = [];

	private int _selectedIndex = -1;

	[Signal]
	public delegate void DemoSelectedEventHandler(PackedScene scene);

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

		// An exported array can hold empty slots while a demo is being added, so a missing entry is skipped
		// rather than left to fail on the first property read.
		foreach (DemoEntry demo in Demos)
		{
			if (demo is null)
			{
				continue;
			}

			int index = _entries.Count;

			var card = new Button
			{
				Text = demo.Title,
				Alignment = HorizontalAlignment.Left,
				CustomMinimumSize = new Vector2(0, 36),
			};

			card.Pressed += () => Select(index);

			_entries.Add(demo);
			_cards.Add(card);
			CardList.AddChild(card);
		}

		if (_entries.Count == 0)
		{
			PlayButton.Disabled = true;
			return;
		}

		Select(0);
		_cards[0].GrabFocus();
	}

	private void Select(int index)
	{
		_selectedIndex = index;

		DemoEntry demo = _entries[index];

		TitleLabel.Text = demo.Title;
		TitleLabel.AddThemeColorOverride(_fontColor, demo.Accent);
		TaglineLabel.Text = demo.Tagline;
		BlurbLabel.Text = demo.Blurb;

		string bullet = $"[color=#{demo.Accent.ToHtml(false)}]•[/color]  ";
		var highlights = new StringBuilder();

		foreach (string highlight in demo.Highlights)
		{
			// Highlights are plain data rendered into a BBCode list, so an opening bracket is escaped instead of
			// being parsed as a tag and swallowing the rest of the line. Blurb stays BBCode on purpose.
			highlights.Append(bullet).Append(highlight.Replace("[", "[lb]")).Append('\n');
		}

		HighlightsLabel.Text = highlights.ToString();

		for (int i = 0; i < _cards.Count; i++)
		{
			_cards[i].AddThemeColorOverride(_fontColor, i == index ? demo.Accent : _unselectedCardColor);
		}

		PlayButton.Disabled = demo.Scene is null;
	}

	private void OnPlayPressed()
	{
		if (_selectedIndex < 0 || _entries[_selectedIndex].Scene is null)
		{
			return;
		}

		EmitSignal(SignalName.DemoSelected, _entries[_selectedIndex].Scene!);
	}
}
