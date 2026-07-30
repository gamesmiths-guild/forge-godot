// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

/// <summary>
/// A <see cref="ForgeAbilityBehavior"/> implementation that creates a <see cref="GraphAbilityBehavior"/> from a
/// serialized <see cref="StatescriptGraph"/> resource. The graph is built once and cached, then shared across all
/// ability instances using the Flyweight pattern. Each <see cref="GraphAbilityBehavior"/> creates its own
/// <see cref="GraphProcessor"/> with independent <see cref="GraphContext"/> state.
/// </summary>
/// <remarks>
/// If any node in the graph uses an <see cref="AbilityActivationDataResolverResource"/>, the behavior automatically
/// detects the associated <see cref="IAbilityActivationDataProvider"/> implementation and produces the matching
/// <see cref="GraphAbilityBehavior{TData}"/> directly. When no activation data resolver is present, a plain
/// <see cref="GraphAbilityBehavior"/> is created.
/// </remarks>
[Tool]
[GlobalClass]
[Icon("uid://b6yrjb46fluw3")]
public partial class StatescriptAbilityBehavior : ForgeAbilityBehavior
{
	private readonly Callable _statescriptChangedCallable;

	private Graph? _cachedGraph;

	private IAbilityActivationDataProvider? _cachedProvider;

	private StatescriptGraph? _statescript;

	private bool _providerResolved;

	/// <summary>
	/// Gets or sets the Statescript graph resource that defines the ability's behavior.
	/// </summary>
	[Export]
	public StatescriptGraph? Statescript
	{
		get => _statescript;
		set
		{
			if (ReferenceEquals(_statescript, value))
			{
				return;
			}

			UnsubscribeFromStatescriptChanged(_statescript);
			_statescript = value;
			SubscribeToStatescriptChanged(_statescript);
			InvalidateCachedBehavior();
			EmitChanged();
		}
	}

	public StatescriptAbilityBehavior()
	{
		_statescriptChangedCallable = new Callable(this, nameof(OnStatescriptChanged));
	}

	/// <inheritdoc/>
	public override IAbilityBehavior GetBehavior()
	{
		if (Statescript is null)
		{
			GD.PushError("StatescriptAbilityBehavior: Statescript is null.");
			throw new InvalidOperationException("StatescriptAbilityBehavior requires a valid Statescript assigned.");
		}

		_cachedGraph ??= StatescriptGraphBuilder.Build(Statescript);

		if (!_providerResolved)
		{
			_cachedProvider = FindActivationDataProvider(Statescript);
			_providerResolved = true;
		}

		if (_cachedProvider is not null)
		{
			Type behaviorType = typeof(GraphAbilityBehavior<>).MakeGenericType(_cachedProvider.DataType);
			return (IAbilityBehavior)Activator.CreateInstance(behaviorType, _cachedGraph)!;
		}

		return new GraphAbilityBehavior(_cachedGraph);
	}

	// Only the read-side resolver counts: it means this graph reads the data its own ability was activated with, so the
	// behavior must be the typed one. A send-side AbilityActivatorResolverResource describes data going out to some
	// other ability and says nothing about this graph's own activation data.
	private static IAbilityActivationDataProvider? FindActivationDataProvider(StatescriptGraph graph)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				if (binding.Resolver
						is AbilityActivationDataResolverResource { ProviderClassName.Length: > 0 } resolver
					&& AbilityActivationDataProviderRegistry.TryGet(
						resolver.ProviderClassName,
						out IAbilityActivationDataProvider provider))
				{
					return provider;
				}
			}
		}

		return null;
	}

	private void InvalidateCachedBehavior()
	{
		_cachedGraph = null;
		_cachedProvider = null;
		_providerResolved = false;
	}

	private void SubscribeToStatescriptChanged(StatescriptGraph? graph)
	{
		if (graph?.IsConnected(Resource.SignalName.Changed, _statescriptChangedCallable) != false)
		{
			return;
		}

		graph.Connect(Resource.SignalName.Changed, _statescriptChangedCallable);
	}

	private void UnsubscribeFromStatescriptChanged(StatescriptGraph? graph)
	{
		if (graph?.IsConnected(Resource.SignalName.Changed, _statescriptChangedCallable) != true)
		{
			return;
		}

		graph.Disconnect(Resource.SignalName.Changed, _statescriptChangedCallable);
	}

	private void OnStatescriptChanged()
	{
		InvalidateCachedBehavior();
		EmitChanged();
	}
}
