// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Tests.Editor;
using Godot;

namespace Gamesmiths.Forge.Godot;

/// <summary>
/// Implements the plugin's editor-test hook.
/// </summary>
/// <remarks>
/// The hook has to hang off the plugin because Godot's CLI cannot enable an editor plugin for a single run, and an
/// <c>EditorScript</c> cannot be driven by <c>--script</c> either. Keeping the implementation here rather than in
/// <c>addons/forge/</c> leaves the shipped plugin with only an unimplemented <c>partial void</c> declaration, which
/// the compiler removes along with its call site.
/// </remarks>
public partial class ForgePluginLoader
{
	partial void RunEditorTestsIfRequested()
	{
		if (!EditorTestRunner.ShouldRun())
		{
			return;
		}

		// Deferred so the docks are in the tree before anything drives them.
		Callable.From(() => EditorTestRunner.RunAndQuit(_statescriptGraphEditorDock!, GetUndoRedo())).CallDeferred();
	}
}
#endif
