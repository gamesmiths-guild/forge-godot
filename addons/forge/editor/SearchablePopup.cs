// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Turns on Godot's popup search bar (4.7+) and caps a popup's height, so a long list can be filtered by typing
/// instead of running off the screen.
/// </summary>
/// <remarks>
/// Shared so every long picker in the plugin behaves the same way, whether it hangs off an
/// <see cref="OptionButton"/> or a <see cref="MenuButton"/>.
/// </remarks>
internal static class SearchablePopup
{
	// Lists shorter than this keep the plain dropdown (no search bar), mirroring Godot's own threshold behavior.
	private const int MinItemCountForSearch = 10;

	private const int MaxPopupHeight = 400;

	/// <summary>
	/// Applies the shared search bar and height cap to a popup.
	/// </summary>
	/// <param name="popup">The popup to configure.</param>
	/// <param name="window">The window the popup opens from, for the screen its height is capped against.</param>
	public static void Configure(PopupMenu popup, Window window)
	{
		popup.SearchBarEnabled = true;
		popup.SearchBarMinItemCount = MinItemCountForSearch;

		Rect2I usableScreen = DisplayServer.ScreenGetUsableRect(window.CurrentScreen);
		popup.MaxSize = new Vector2I(
			usableScreen.Size.X,
			(int)(MaxPopupHeight * EditorInterface.Singleton.GetEditorScale()));
	}
}
#endif
