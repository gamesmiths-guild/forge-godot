// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Registry of available <see cref="NodeEditorProperty"/> implementations. Resolver editors are discovered
/// automatically via reflection. Any concrete subclass of <see cref="NodeEditorProperty"/> in the executing assembly is
/// registered and becomes available in node input property dropdowns.
/// </summary>
/// <remarks>
/// Metadata (display name, type id, shape support, per-type compatibility) is probed once per editor type and cached.
/// Probing instantiates a temporary editor control, so uncached queries are costly; the caches keep dropdown population
/// and node rebuilds from re-instantiating every registered editor per query.
/// </remarks>
internal static class StatescriptResolverRegistry
{
	private static readonly List<Func<NodeEditorProperty>> _factories = [];

	private static readonly Dictionary<Func<NodeEditorProperty>, ResolverEditorFactoryMetadata> _metadataByFactory = [];

	private static readonly Dictionary<Type, Func<NodeEditorProperty>[]> _compatibleFactoriesByType = [];

	// Intentional default resolver per scalar input type. Types not listed here fall back to the Variant constant
	// editor when available, otherwise the first compatible editor in registration order.
	private static readonly Dictionary<Type, string> _defaultScalarResolverIds = new()
	{
		[typeof(IForgeEntity)] = "AbilityOwner",
		[typeof(AbilityData)] = "AbilityData",
		[typeof(AbilityHandle)] = "GetAbilityHandle",
		[typeof(ActiveEffectHandle)] = "Variable",
		[typeof(Effect)] = "Effect",
		[typeof(Tag)] = "Tag",
		[typeof(PackedScene)] = "ScenePicker",

		// Node references are always produced at runtime - by a spawn node, or by a lookup resolver - so a variable
		// read is the only sensible authoring default.
		[typeof(Node)] = "Variable",

		// Provider-backed marker types, one per optional provider input. Every one of these MUST be listed:
		// RandomElementResolverEditor reports compatibility with any reference type, so it lands in these dropdowns
		// too, and without a pin the selection would fall through to reflection registration order rather than the
		// provider editor the input exists for. Only the listener-side EventPayloadWriter is absent, because
		// EventListenerNodeEditor renders that slot itself instead of as a resolver row.
		[typeof(AbilityActivator)] = "AbilityActivator",
		[typeof(Dictionary<StringKey, object>)] = "CueCustomParameters",
		[typeof(EffectApplicationContext)] = "EffectContextData",
		[typeof(EventPayloadRaiser)] = "EventPayload",
	};

	static StatescriptResolverRegistry()
	{
		Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		foreach (Type type in allTypes.Where(
			x => x.IsSubclassOf(typeof(NodeEditorProperty)) && !x.IsAbstract))
		{
			Type captured = type;
			_factories.Add(() => (NodeEditorProperty)Activator.CreateInstance(captured)!);
		}
	}

	/// <summary>
	/// Gets factory functions for all resolver editors compatible with the given expected type.
	/// </summary>
	/// <param name="expectedType">The type expected by the node input property.</param>
	/// <returns>A list of compatible resolver editor factories.</returns>
	public static List<Func<NodeEditorProperty>> GetCompatibleFactories(Type expectedType)
	{
		if (!_compatibleFactoriesByType.TryGetValue(expectedType, out Func<NodeEditorProperty>[]? compatible))
		{
			compatible = [.. _factories.Where(factory => IsCompatibleFactory(factory, expectedType))];
			_compatibleFactoriesByType[expectedType] = compatible;
		}

		// Callers filter the result in place (RemoveAll), so hand out a fresh copy of the cached set.
		return [.. compatible];
	}

	public static int GetDefaultFactoryIndex(List<Func<NodeEditorProperty>> factories, Type expectedType, bool isArray)
	{
		if (!isArray && _defaultScalarResolverIds.TryGetValue(expectedType, out string? preferredResolverId))
		{
			for (int i = 0; i < factories.Count; i++)
			{
				if (GetResolverTypeId(factories[i]) == preferredResolverId)
				{
					return i;
				}
			}
		}

		for (int i = 0; i < factories.Count; i++)
		{
			string resolverTypeId = GetResolverTypeId(factories[i]);

			if (isArray)
			{
				if (resolverTypeId is "Variant" or "Variable")
				{
					return i;
				}
			}
			else if (resolverTypeId == "Variant")
			{
				return i;
			}
		}

		return 0;
	}

	public static string GetDisplayName(Func<NodeEditorProperty> factory)
	{
		return GetMetadata(factory).DisplayName;
	}

	public static string GetResolverTypeId(Func<NodeEditorProperty> factory)
	{
		return GetMetadata(factory).ResolverTypeId;
	}

	public static bool IsCompatibleFactory(Func<NodeEditorProperty> factory, Type expectedType)
	{
		ResolverEditorFactoryMetadata metadata = GetMetadata(factory);

		if (!metadata.CompatibilityByType.TryGetValue(expectedType, out bool compatible))
		{
			compatible = UseTemporaryEditor(factory, editor => editor.IsCompatibleWith(expectedType));
			metadata.CompatibilityByType[expectedType] = compatible;
		}

		return compatible;
	}

	public static bool SupportsArrayValues(Func<NodeEditorProperty> factory)
	{
		return GetMetadata(factory).SupportsArrayValues;
	}

	public static bool SupportsScalarValues(Func<NodeEditorProperty> factory)
	{
		return GetMetadata(factory).SupportsScalarValues;
	}

	public static bool RequiresIterationScope(Func<NodeEditorProperty> factory)
	{
		return GetMetadata(factory).RequiresIterationScope;
	}

	private static ResolverEditorFactoryMetadata GetMetadata(Func<NodeEditorProperty> factory)
	{
		if (_metadataByFactory.TryGetValue(factory, out ResolverEditorFactoryMetadata? metadata))
		{
			return metadata;
		}

		metadata = UseTemporaryEditor(
			factory,
			static editor => new ResolverEditorFactoryMetadata(
				editor.DisplayName,
				editor.ResolverTypeId,
				editor.SupportsScalarValues,
				editor.SupportsArrayValues,
				editor.RequiresIterationScope));

		_metadataByFactory[factory] = metadata;
		return metadata;
	}

	private static TResult UseTemporaryEditor<TResult>(
		Func<NodeEditorProperty> factory,
		Func<NodeEditorProperty, TResult> selector)
	{
		NodeEditorProperty editor = factory();

		try
		{
			return selector(editor);
		}
		finally
		{
			if (global::Godot.GodotObject.IsInstanceValid(editor))
			{
				editor.Free();
			}
		}
	}
}
#endif
