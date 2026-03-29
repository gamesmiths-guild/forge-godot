// Copyright © Gamesmiths Guild.

using System;
using System.Reflection;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

/// <summary>
/// A <see cref="ForgeAbilityBehavior"/> implementation that creates a <see cref="GraphAbilityBehavior"/> from a
/// serialized <see cref="StatescriptGraph"/> resource. The graph is built once and cached, then shared across all
/// ability instances using the Flyweight pattern. Each <see cref="GraphAbilityBehavior"/> creates its own
/// <see cref="GraphProcessor"/> with independent <see cref="GraphContext"/> state.
/// </summary>
/// <remarks>
/// If any node in the graph uses an <see cref="ActivationDataResolverResource"/>, the behavior automatically detects
/// the associated <see cref="IActivationDataProvider"/> implementation and produces a
/// <see cref="GraphAbilityBehavior{TData}"/> with a data binder that maps activation data fields into graph variables.
/// When no activation data resolver is present, a plain <see cref="GraphAbilityBehavior"/> (without data
/// support) is created.
/// </remarks>
[Tool]
[GlobalClass]
[Icon("uid://b6yrjb46fluw3")]
public partial class StatescriptAbilityBehavior : ForgeAbilityBehavior
{
	private Graph? _cachedGraph;

	private IActivationDataProvider? _cachedProvider;

	private bool _providerResolved;

	/// <summary>
	/// Gets or sets the Statescript graph resource that defines the ability's behavior.
	/// </summary>
	[Export]
	public StatescriptGraph? Statescript { get; set; }

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
			return _cachedProvider.CreateBehavior(_cachedGraph);
		}

		return new GraphAbilityBehavior(_cachedGraph);
	}

	private static IActivationDataProvider? FindActivationDataProvider(StatescriptGraph graph)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				if (binding.Resolver is ActivationDataResolverResource { ProviderClassName.Length: > 0 } resolver)
				{
					return InstantiateProvider(resolver.ProviderClassName);
				}
			}
		}

		return null;
	}

	private static IActivationDataProvider? InstantiateProvider(string className)
	{
		Type? type = Array.Find(
			Assembly.GetExecutingAssembly().GetTypes(),
			x => typeof(IActivationDataProvider).IsAssignableFrom(x)
				&& !x.IsAbstract
				&& !x.IsInterface
				&& x.Name == className);

		if (type is null)
		{
			return null;
		}

		return Activator.CreateInstance(type) as IActivationDataProvider;
	}
}
