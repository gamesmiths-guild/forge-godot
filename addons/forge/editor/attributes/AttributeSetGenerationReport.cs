// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

/// <summary>
/// What a run of <see cref="AttributeSetCodeGenerator.RegenerateAll"/> did, so the outcome can be reported instead of
/// leaving the user to guess whether anything happened.
/// </summary>
internal sealed class AttributeSetGenerationReport
{
	/// <summary>Gets the names of the sets whose class was written.</summary>
	public List<string> GeneratedSets { get; } = [];

	/// <summary>Gets the files removed because their definition is gone or no longer valid.</summary>
	public List<string> RemovedFiles { get; } = [];

	/// <summary>Gets the problems that stopped a definition from being generated.</summary>
	public List<string> Errors { get; } = [];
}
#endif
