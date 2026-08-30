// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Base for every resolver that reads the state of one or more input actions.
/// </summary>
/// <remarks>
/// <para>Handles the one thing all of them share: an action name is authored as free text, so it can name an action the
/// project's <c>InputMap</c> does not have. Godot pushes an error of its own for every such read, which on a resolver
/// running each tick is an error per frame, so the names are checked first and reported once.</para>
/// <para>Input is read straight from the device, which makes these client-local. An authoritative or networked game
/// should sample aim and button state once at activation into the ability's activation data rather than polling them
/// inside a graph the server runs.</para>
/// </remarks>
/// <param name="actionNames">The actions this resolver reads. All of them must exist for it to resolve.</param>
internal abstract class InputResolverBase(params string[] actionNames) : IPropertyResolver
{
	private readonly StringName[] _actionNames = Array.ConvertAll(actionNames, actionName => (StringName)actionName);

	private bool _reportedMissingAction;

	/// <inheritdoc/>
	public abstract Type ValueType { get; }

	/// <summary>
	/// Reads this resolver's value off the device. Called only once every action it names exists.
	/// </summary>
	/// <returns>The resolved value.</returns>
	protected abstract Variant128 ResolveInput();

#pragma warning disable SA1202 // Elements should be ordered by access
	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		foreach (StringName actionName in _actionNames)
		{
			if (!InputMap.HasAction(actionName))
			{
				ReportMissingActionOnce(actionName);
				return default;
			}
		}

		return ResolveInput();
	}
#pragma warning restore SA1202 // Elements should be ordered by access

	private void ReportMissingActionOnce(StringName actionName)
	{
		if (_reportedMissingAction)
		{
			return;
		}

		_reportedMissingAction = true;

		GD.PushWarning(
			$"Statescript: {GetType().Name} names the input action [{actionName}], which the project's Input Map " +
			"does not have. Resolving to a default value.");
	}
}
