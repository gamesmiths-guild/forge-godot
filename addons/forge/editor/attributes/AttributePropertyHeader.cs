// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

/// <summary>
/// Paints the header band behind an <see cref="EditorProperty"/>'s name row.
/// </summary>
/// <remarks>
/// <para>
/// The engine draws a property's name text from its own <c>NOTIFICATION_DRAW</c>, which always runs before any script
/// drawing, so a property cannot paint a background behind its own name. A child <see cref="CanvasItem"/> with
/// <see cref="CanvasItem.ShowBehindParent"/> can, because it is drawn before its parent's own commands.
/// </para>
/// <para>
/// It is a <see cref="Node2D"/> rather than a <see cref="Control"/> on purpose. <c>EditorProperty</c> lays out its
/// children through <c>Container.as_sortable_control</c>, which casts to <see cref="Control"/> and skips anything else,
/// so a non-Control child is ignored by the layout pass entirely. A Control here would instead be counted as the
/// property's editor and fitted into the name row, shrinking the space the name is drawn in.
/// </para>
/// </remarks>
[Tool]
internal sealed partial class AttributePropertyHeader : Node2D
{
	private StyleBox? _style;
	private Vector2 _headerSize;

	/// <inheritdoc/>
	public override void _Ready()
	{
		ShowBehindParent = true;
	}

	/// <summary>
	/// Sets the band's appearance.
	/// </summary>
	/// <param name="style">The stylebox to paint, or null to paint nothing.</param>
	public void SetStyle(StyleBox? style)
	{
		_style = style;
		QueueRedraw();
	}

	/// <summary>
	/// Sets the area the band covers, which is the property's name row.
	/// </summary>
	/// <param name="headerSize">The size of the name row.</param>
	public void SetHeaderSize(Vector2 headerSize)
	{
		if (_headerSize == headerSize)
		{
			return;
		}

		_headerSize = headerSize;
		QueueRedraw();
	}

	/// <inheritdoc/>
	public override void _Draw()
	{
		if (_style is null || _headerSize.X <= 0 || _headerSize.Y <= 0)
		{
			return;
		}

		DrawStyleBox(_style, new Rect2(Vector2.Zero, _headerSize));
	}
}
#endif
