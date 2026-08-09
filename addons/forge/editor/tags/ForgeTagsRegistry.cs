// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// The editor's shared view of the project's gameplay tags.
/// </summary>
/// <remarks>
/// <para>
/// This caches the sources and the hierarchies built from them, and raises <see cref="Changed"/> so every open editor
/// refreshes together.
/// </para>
/// <para>
/// It is static because its consumers - inspector property editors, resolver editors inside graph nodes - are created
/// and destroyed constantly, with no shared owner to hand an instance down from.
/// </para>
/// </remarks>
internal static class ForgeTagsRegistry
{
	private static List<SourceEntry>? _sources;
	private static TagsManager? _mergedTags;
	private static string[]? _registeredTags;
	private static string[] _cachedReferences = [];

	/// <summary>
	/// Raised whenever the tags or the configured sources change. Handlers should rebuild from the registry rather
	/// than assume what changed.
	/// </summary>
#pragma warning disable S3264 // Raised through RaiseChanged, which walks the invocation list to skip freed listeners.
	public static event Action? Changed;
#pragma warning restore S3264

	/// <summary>
	/// Gets the configured sources in display order, including ones whose file is missing.
	/// </summary>
	public static IReadOnlyList<SourceEntry> Sources
	{
		get
		{
			EnsureBuilt();
			return _sources;
		}
	}

	/// <summary>
	/// Gets the hierarchy built from the union of every configured source.
	/// </summary>
	public static TagsManager MergedTags
	{
		get
		{
			EnsureBuilt();
			return _mergedTags;
		}
	}

	/// <summary>
	/// Gets every tag key registered by the project, with duplicates removed.
	/// </summary>
	public static string[] RegisteredTags
	{
		get
		{
			EnsureBuilt();
			return _registeredTags;
		}
	}

	/// <summary>
	/// Gets the display names of the sources that put <paramref name="completeTagKey"/> in the tree, either by
	/// declaring it or by declaring a descendant of it.
	/// </summary>
	/// <param name="completeTagKey">The full dotted key of the tag.</param>
	/// <returns>The declaring sources, in display order.</returns>
	public static IReadOnlyList<string> GetDeclaringSources(string completeTagKey)
	{
		EnsureBuilt();

		return
		[
			.. _sources
				.Where(entry => entry.Resource?.DeclaresTagOrDescendant(completeTagKey) == true)
				.Select(entry => entry.DisplayName),
		];
	}

	/// <summary>
	/// Drops the cache and notifies every listener. The rebuild is deferred until something asks for it, so
	/// invalidating from a filesystem signal costs nothing when no tag editor is open.
	/// </summary>
	public static void Invalidate()
	{
		DropCache();
		RaiseChanged();
	}

	/// <summary>
	/// Re-reads the configured references and invalidates only if they actually differ, so that unrelated project
	/// settings changes do not tear down every open tag tree.
	/// </summary>
	public static void InvalidateIfSourcesChanged()
	{
		if (ForgeSettings.GetSourceReferences().SequenceEqual(_cachedReferences, StringComparer.Ordinal))
		{
			return;
		}

		Invalidate();
	}

	/// <summary>
	/// Invalidates when any of <paramref name="paths"/> belongs to a configured source.
	/// </summary>
	/// <param name="paths">Resource paths reported by the editor filesystem.</param>
	public static void InvalidateIfAny(string[] paths)
	{
		if (_sources is null)
		{
			return;
		}

		List<SourceEntry> sources = _sources;

		if (Array.Exists(paths, path =>
			sources.Exists(entry => string.Equals(entry.ResourcePath, path, StringComparison.OrdinalIgnoreCase))))
		{
			Invalidate();
		}
	}

	/// <summary>
	/// Releases everything the registry holds, including its listeners. Call this when the plugin unloads.
	/// </summary>
	public static void Release()
	{
		DropCache();
		Changed = null;
	}

	[MemberNotNull(nameof(_sources), nameof(_mergedTags), nameof(_registeredTags))]
	private static void EnsureBuilt()
	{
		if (_sources is not null && _mergedTags is not null && _registeredTags is not null)
		{
			return;
		}

		string[] references = ForgeSettings.GetSourceReferences();
		var sources = new List<SourceEntry>(references.Length);

		foreach (string reference in references)
		{
			sources.Add(BuildEntry(reference));
		}

		_sources = sources;
		_cachedReferences = references;

		_registeredTags =
		[
			.. sources
				.Where(entry => entry.Resource is not null)
				.SelectMany(entry => entry.Resource!.RegisteredTags)
				.Distinct(StringComparer.OrdinalIgnoreCase),
		];

		_mergedTags = new TagsManager(_registeredTags);
	}

	private static SourceEntry BuildEntry(string reference)
	{
		string path = ForgeSettings.ResolveReference(reference);

		// Deliberately the default cache mode. Replace would re-read the file into the instance everything else already
		// holds, and the editor reads that as the resource having been touched - which is enough to make it ask
		// whether to save unmodified resources on every shutdown. Re-reading is not needed anyway: when a file really
		// does change on disk the editor reloads it and fires ResourcesReload, which is what invalidates this cache.
		ForgeTagsSource? resource = ResourceLoader.Exists(path)
			? ResourceLoader.Load(path, null, ResourceLoader.CacheMode.Reuse) as ForgeTagsSource
			: null;

		string displayName = path.StartsWith("res://", StringComparison.Ordinal)
			? path.GetFile().GetBaseName()
			: reference;

		return new SourceEntry(
			reference,
			path,
			displayName,
			resource,
			resource is null ? null : new TagsManager([.. resource.RegisteredTags]));
	}

	private static void DropCache()
	{
		if (_sources is not null)
		{
			foreach (SourceEntry entry in _sources)
			{
				entry.Tags?.DestroyTagTree();
			}
		}

		_mergedTags?.DestroyTagTree();

		_sources = null;
		_mergedTags = null;
		_registeredTags = null;
	}

	/// <summary>
	/// Invokes the listeners, dropping any whose owner Godot has already freed.
	/// </summary>
	/// <remarks>
	/// Editor controls are freed and rebuilt on every assembly reload and every inspector rebuild. Without this, a
	/// listener that outlived its control would call into a disposed object the next time the tags changed, which
	/// Godot reports as an error from a completely unrelated place.
	/// </remarks>
	private static void RaiseChanged()
	{
		Delegate[]? handlers = Changed?.GetInvocationList();

		if (handlers is null)
		{
			return;
		}

		foreach (Delegate handler in handlers)
		{
			var listener = (Action)handler;

			if (handler.Target is GodotObject owner && !GodotObject.IsInstanceValid(owner))
			{
				Changed -= listener;
				continue;
			}

			listener();
		}
	}
}
#endif
