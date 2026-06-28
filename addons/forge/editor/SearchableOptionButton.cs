// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// A drop-in <see cref="OptionButton"/> that turns on Godot's popup search bar (4.7+) so long pickers, such as the
/// resolver list on a numeric input, can be filtered by typing. The bar only appears once the list reaches
/// <see cref="SearchBarMinItemCount"/> items, so short dropdowns look and behave exactly like a plain
/// <see cref="OptionButton"/>.
/// </summary>
[Tool]
internal sealed partial class SearchableOptionButton : OptionButton
{
	// Lists shorter than this keep the plain dropdown (no search bar), mirroring Godot's own threshold behavior.
	private const int MinItemCountForSearch = 10;

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		PopupMenu popup = GetPopup();
		popup.SearchBarEnabled = true;
		popup.SearchBarMinItemCount = MinItemCountForSearch;
	}
}
#endif
