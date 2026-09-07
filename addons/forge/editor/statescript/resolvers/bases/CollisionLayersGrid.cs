// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// The thirty-two bit collision layer grid, drawn the way Godot's inspector draws the one behind
/// <c>collision_layer</c> and <c>collision_mask</c>: blocks of four by two squares laid across the available width,
/// with an arrow that unfolds the blocks which did not fit.
/// </summary>
/// <remarks>
/// A port of the engine's <c>EditorPropertyLayersGrid</c> rather than an approximation of it, down to the block
/// packing and the alpha steps, so a mask picked in a Statescript node looks and behaves like the same mask picked on
/// the body it will be compared against. Layer names are supplied by the owner, which knows whether the slot is 2D or
/// 3D.
/// </remarks>
[Tool]
internal sealed partial class CollisionLayersGrid : Control
{
	/// <summary>
	/// How many layers a physics world has. Fixed by the engine: the layer field is a 32 bit integer.
	/// </summary>
	public const int LayerCount = 32;

	// Four columns by two rows per block, which is what the engine uses for physics layers. Render layers group by
	// five, but nothing here picks those.
	private const int LayerGroupSize = 4;

	private const int HoveredIndexNone = -1;

	private readonly List<Rect2> _flagRects = [];

	private string[] _names = [];
	private string[] _tooltips = [];
	private Rect2 _expandRect;
	private bool _expandHovered;
	private int _expansionRows;
	private int _hoveredIndex = HoveredIndexNone;
	private bool _dragging;
	private bool _draggingValueToSet;

	/// <summary>
	/// Raised when a square is toggled, with the whole bit field.
	/// </summary>
	public event Action<uint>? FlagChanged;

	/// <summary>
	/// Raised when the expansion arrow is used, so the owner can persist the fold state.
	/// </summary>
	public event Action? ExpandedChanged;

	/// <summary>
	/// Gets or sets the selected bits.
	/// </summary>
	public uint Value { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the blocks that did not fit on one row are shown.
	/// </summary>
	public bool Expanded { get; set; }

	/// <summary>
	/// Gets the name of a layer, or an empty string when the project has not named it.
	/// </summary>
	/// <param name="layerIndex">The zero-based layer index.</param>
	/// <returns>The layer name.</returns>
	public string GetLayerName(int layerIndex)
	{
		return layerIndex >= 0 && layerIndex < _names.Length ? _names[layerIndex] : string.Empty;
	}

	/// <summary>
	/// Sets the layer names shown in tooltips, and their tooltip lines.
	/// </summary>
	/// <param name="names">The names, empty where the project has not named the layer.</param>
	/// <param name="tooltips">The tooltip text per layer.</param>
	public void SetLayerNames(string[] names, string[] tooltips)
	{
		_names = names;
		_tooltips = tooltips;
		QueueRedraw();
	}

	/// <summary>
	/// Clears the delegate fields so a hot reload does not try to serialize them.
	/// </summary>
	public void ClearCallbacks()
	{
		FlagChanged = null;
		ExpandedChanged = null;
	}

	/// <inheritdoc/>
	public override Vector2 _GetMinimumSize()
	{
		Vector2 minSize = GetGridSize();

		if (Expanded)
		{
			int bsize = GetBlockSquareSize(minSize.Y);

			for (int i = 0; i < _expansionRows; i++)
			{
				minSize.Y += (2 * (bsize + 1)) + 3;
			}
		}

		return minSize;
	}

	/// <inheritdoc/>
	public override string _GetTooltip(Vector2 atPosition)
	{
		for (int i = 0; i < _flagRects.Count; i++)
		{
			if (i < _tooltips.Length && _flagRects[i].HasPoint(atPosition))
			{
				return _tooltips[i];
			}
		}

		return string.Empty;
	}

	/// <inheritdoc/>
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion)
		{
			UpdateHovered(motion.Position);

			if (_dragging
				&& _hoveredIndex != HoveredIndexNone
				&& _draggingValueToSet != ((Value & (1u << _hoveredIndex)) != 0))
			{
				Value ^= 1u << _hoveredIndex;
				FlagChanged?.Invoke(Value);
				QueueRedraw();
			}

			return;
		}

		if (@event is not InputEventMouseButton button || button.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (!button.Pressed)
		{
			_dragging = false;
			return;
		}

		UpdateHovered(button.Position);
		bool replaceMode = button.IsCommandOrControlPressed();
		UpdateFlag(replaceMode);

		if (!replaceMode && _hoveredIndex != HoveredIndexNone)
		{
			_dragging = true;
			_draggingValueToSet = (Value & (1u << _hoveredIndex)) != 0;
		}
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		if (what != NotificationMouseExit)
		{
			return;
		}

		if (_expandHovered)
		{
			_expandHovered = false;
			QueueRedraw();
		}

		if (_hoveredIndex != HoveredIndexNone)
		{
			_hoveredIndex = HoveredIndexNone;
			QueueRedraw();
		}

		_dragging = false;
	}

	/// <inheritdoc/>
	public override void _Draw()
	{
		Vector2 gridSize = GetGridSize();
		gridSize.X = Size.X;

		_flagRects.Clear();

		int previousExpansionRows = _expansionRows;
		_expansionRows = 0;

		int bsize = GetBlockSquareSize(gridSize.Y);
		int h = (bsize * 2) + 1;

		Color color = GetThemeColor("highlight_color", "Editor");

		Color textColor = GetThemeColor("font_color", "Editor");
		textColor.A *= 0.5f;

		Color textColorOn = GetThemeColor("font_hover_color", "Editor");
		textColorOn.A *= 0.7f;

		Font font = GetThemeFont("font", "Label");
		int fontSize = GetThemeFontSize("font_size", "Label");

		var blockOffset = new Vector2(4, (int)(gridSize.Y - h) / 2);
		Vector2 arrowPosition = Vector2.Zero;
		int layerIndex = 0;

		while (true)
		{
			Vector2 offset = blockOffset;

			for (int row = 0; row < 2; row++)
			{
				for (int column = 0; column < LayerGroupSize; column++)
				{
					bool on = (Value & (1u << layerIndex)) != 0;
					var rect = new Rect2(offset, new Vector2(bsize, bsize));

					color.A = on ? 0.6f : 0.2f;

					if (layerIndex == _hoveredIndex)
					{
						color.A += 0.15f;
					}

					DrawRect(rect, color);
					_flagRects.Add(rect);

					DrawString(
						font,
						rect.Position + new Vector2(0, rect.Size.Y * 0.75f),
						(layerIndex + 1).ToString(CultureInfo.InvariantCulture),
						HorizontalAlignment.Center,
						rect.Size.X,
						fontSize,
						on ? textColorOn : textColor);

					offset.X += bsize + 1;
					layerIndex++;
				}

				offset.X = blockOffset.X;
				offset.Y += bsize + 1;
			}

			if (layerIndex >= LayerCount)
			{
				TrackArrowPosition(ref arrowPosition);
				break;
			}

			int blockWidth = LayerGroupSize * (bsize + 1);
			blockOffset.X += blockWidth + 3;

			if (blockOffset.X + blockWidth + 12 > gridSize.X)
			{
				TrackArrowPosition(ref arrowPosition);
				_expansionRows++;

				if (!Expanded)
				{
					break;
				}

				blockOffset.X = 4;
				blockOffset.Y += (2 * (bsize + 1)) + 3;
			}
		}

		if (_expansionRows != previousExpansionRows && Expanded)
		{
			UpdateMinimumSize();
		}

		if (_expansionRows == 0 && layerIndex == LayerCount)
		{
			// The whole grid fit, so there is nothing to unfold.
			_expandRect = default;
			return;
		}

		Texture2D arrow = GetThemeIcon("arrow", "Tree");
		Color arrowColor = GetThemeColor("highlight_color", "Editor");
		arrowColor.A = _expandHovered ? 1.0f : 0.6f;

		arrowPosition.X += 2.0f;
		arrowPosition.Y -= arrow.GetHeight();

		var arrowRect = new Rect2(arrowPosition, arrow.GetSize());
		_expandRect = arrowRect;

		if (Expanded)
		{
			// Flipped vertically to point the other way, which is how the engine draws the unfolded state.
			arrowRect.Size = new Vector2(arrowRect.Size.X, -arrowRect.Size.Y);
		}

		arrow.DrawRect(GetCanvasItem(), arrowRect, tile: false, arrowColor);
	}

	private static int GetBlockSquareSize(float gridHeight)
	{
		return (int)(gridHeight * 80 / 100 / 2);
	}

	private Vector2 GetGridSize()
	{
		Font font = GetThemeFont("font", "Label");
		int fontSize = GetThemeFontSize("font_size", "Label");
		return new Vector2(0, font.GetHeight(fontSize) * 3);
	}

	private void TrackArrowPosition(ref Vector2 arrowPosition)
	{
		if (_flagRects.Count > 0 && _expansionRows == 0)
		{
			arrowPosition = _flagRects[^1].End;
		}
	}

	private void UpdateHovered(Vector2 position)
	{
		bool expandWasHovered = _expandHovered;
		_expandHovered = _expandRect.HasPoint(position);

		if (_expandHovered != expandWasHovered)
		{
			QueueRedraw();
		}

		if (!_expandHovered)
		{
			for (int i = 0; i < _flagRects.Count; i++)
			{
				if (_flagRects[i].HasPoint(position))
				{
					_hoveredIndex = i;
					QueueRedraw();
					return;
				}
			}
		}

		if (_hoveredIndex != HoveredIndexNone)
		{
			_hoveredIndex = HoveredIndexNone;
			QueueRedraw();
		}
	}

	private void UpdateFlag(bool replace)
	{
		if (_hoveredIndex != HoveredIndexNone)
		{
			if (replace)
			{
				// Solo mode: the hovered layer becomes the only one on, and hitting it again inverts the selection so
				// a second command-click reads as "everything but this one".
				Value = Value == 1u << _hoveredIndex ? ~Value : 1u << _hoveredIndex;
			}
			else
			{
				Value ^= 1u << _hoveredIndex;
			}

			FlagChanged?.Invoke(Value);
			QueueRedraw();
		}
		else if (_expandHovered)
		{
			Expanded = !Expanded;
			UpdateMinimumSize();
			QueueRedraw();
			ExpandedChanged?.Invoke();
		}
	}
}
#endif
