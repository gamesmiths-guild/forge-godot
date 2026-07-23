// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Cached, probe-once metadata for a single resolver editor factory registered in
/// <see cref="StatescriptResolverRegistry"/>. Answering metadata queries from this cache keeps the registry from
/// instantiating a temporary editor control per query.
/// </summary>
/// <param name="displayName">The display name shown in resolver dropdowns.</param>
/// <param name="resolverTypeId">The resolver type identifier used for matching serialized resources.</param>
/// <param name="supportsScalarValues">Whether the editor can author scalar values.</param>
/// <param name="supportsArrayValues">Whether the editor can author array values.</param>
/// <param name="requiresIterationScope">Whether the editor is only valid inside an iteration (lambda) operand.</param>
internal sealed class ResolverEditorFactoryMetadata(
	string displayName,
	string resolverTypeId,
	bool supportsScalarValues,
	bool supportsArrayValues,
	bool requiresIterationScope)
{
	public string DisplayName { get; } = displayName;

	public string ResolverTypeId { get; } = resolverTypeId;

	public bool SupportsScalarValues { get; } = supportsScalarValues;

	public bool SupportsArrayValues { get; } = supportsArrayValues;

	public bool RequiresIterationScope { get; } = requiresIterationScope;

	/// <summary>
	/// Gets the cached per-expected-type compatibility results for this factory.
	/// </summary>
	public Dictionary<Type, bool> CompatibilityByType { get; } = [];
}
#endif
