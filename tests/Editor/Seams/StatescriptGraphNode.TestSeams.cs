// Copyright © Gamesmiths Guild.

#if TOOLS
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Entry points used by the editor test suite to drive a node visual the way its controls do.
/// </summary>
/// <remarks>
/// A partial of the shipping node visual, kept under <c>tests/</c> so it compiles only for this repository's own Debug
/// builds and never reaches an installed copy of the plugin.
/// </remarks>
public partial class StatescriptGraphNode
{
	/// <summary>
	/// Applies a node configuration value through the recording path a Settings control uses.
	/// </summary>
	/// <param name="key">The CustomData key.</param>
	/// <param name="value">The value to store.</param>
	internal void TestOnlySetNodeConfig(string key, Variant value)
	{
		SetNodeConfigWithUndo(key, value, $"Change {key}", rebuildOnChange: false);
	}

	/// <summary>
	/// Applies a width change through the recording path the resize handle uses.
	/// </summary>
	/// <remarks>
	/// Emits both halves of a real drag: <c>ResizeRequest</c> applies and persists the width, <c>ResizeEnd</c> records
	/// the step. Sending only the second would record an action whose "before" state was never written.
	/// </remarks>
	/// <param name="width">The new width.</param>
	internal void TestOnlySetWidth(float width)
	{
		_widthBeforeResize = CustomMinimumSize.X;
		OnResizeRequest(new Vector2(width, 0));
		OnResizeEnd(new Vector2(width, 0));
	}
}
#endif
