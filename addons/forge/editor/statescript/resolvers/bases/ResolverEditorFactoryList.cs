// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// A resolver editor factory list that remembers whether it was built inside an array operation's per-element
/// (lambda) operand. <see cref="NestedResolverEditorUtilities.CreateNestedEditor"/> reads the flag back so
/// iteration scope propagates to child editors without every call site having to thread it explicitly.
/// </summary>
internal sealed class ResolverEditorFactoryList : List<Func<NodeEditorProperty>>
{
	/// <summary>
	/// Gets a value indicating whether the dropdown built from this list sits inside an iteration (lambda) operand.
	/// </summary>
	public bool IterationScope { get; init; }
}
#endif
