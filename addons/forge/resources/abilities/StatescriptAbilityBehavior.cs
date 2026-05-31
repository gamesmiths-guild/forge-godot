// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
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
/// If any node in the graph uses an <see cref="AbilityActivationDataResolverResource"/>, the behavior automatically
/// detects the associated <see cref="IActivationDataProvider"/> implementation and produces the matching
/// <see cref="GraphAbilityBehavior{TData}"/> directly. When no activation data resolver is present, a plain
/// <see cref="GraphAbilityBehavior"/> is created.
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

	public static IActivationDataProvider? InstantiateProvider(string className)
	{
		Type? type = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(GetLoadableTypes)
			.FirstOrDefault(
				x => typeof(IActivationDataProvider).IsAssignableFrom(x)
					&& !x.IsAbstract
					&& !x.IsInterface
					&& (x.AssemblyQualifiedName == className
						|| x.FullName == className
						|| x.Name == className));

		if (type is null)
		{
			return null;
		}

		try
		{
			return Activator.CreateInstance(type) as IActivationDataProvider;
		}
		catch (MissingMethodException)
		{
			GD.PushError(
				$"StatescriptAbilityBehavior: Activation data provider '{type.FullName}' must have a public " +
				"parameterless constructor.");
			return null;
		}
		catch (MemberAccessException)
		{
			GD.PushError(
				$"StatescriptAbilityBehavior: Activation data provider '{type.FullName}' could not be instantiated " +
				"because its constructor is not accessible.");
			return null;
		}
		catch (TargetInvocationException ex)
		{
			string message = ex.InnerException?.Message ?? ex.Message;
			GD.PushError(
				$"StatescriptAbilityBehavior: Activation data provider '{type.FullName}' threw during " +
				$"construction: {message}");
			return null;
		}
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
			Type activationDataType = _cachedProvider.ActivationDataType;
			Type behaviorType = typeof(GraphAbilityBehavior<>).MakeGenericType(activationDataType);
			return (IAbilityBehavior)Activator.CreateInstance(behaviorType, _cachedGraph)!;
		}

		return new GraphAbilityBehavior(_cachedGraph);
	}

	private static IActivationDataProvider? FindActivationDataProvider(StatescriptGraph graph)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				if (binding.Resolver
					is AbilityActivationDataResolverResource { ProviderClassName.Length: > 0 } resolver)
				{
					return InstantiateProvider(resolver.ProviderClassName);
				}
			}
		}

		return null;
	}

	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(type => type is not null)!;
		}
	}
}
