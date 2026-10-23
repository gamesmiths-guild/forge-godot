// Copyright © Gamesmiths Guild.

using System;
using System.Linq;
using Gamesmiths.Forge.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Checks the input actions a resolver names before it reads them, and reports the first one that cannot be read,
/// once.
/// </summary>
/// <remarks>
/// An action name is authored as free text, so it can name an action the project's <c>InputMap</c> does not have, or
/// one the entity's input does not carry. Godot pushes an error of its own for every device read of an unknown action,
/// which on a resolver running each tick is an error per frame, so the names are checked first and reported once.
/// </remarks>
/// <param name="resolverName">The resolver's name, for the warning.</param>
/// <param name="actionNames">The actions the resolver reads.</param>
internal sealed class InputActionChecks(string resolverName, params string[] actionNames)
{
	private readonly string _resolverName = resolverName;
	private readonly string[] _actionNames = actionNames;

	private readonly StringName[] _deviceActionNames =
		Array.ConvertAll(actionNames, actionName => (StringName)actionName);

	private bool _reportedMissingAction;

	/// <summary>
	/// Reports the first action an entity's input does not carry.
	/// </summary>
	/// <param name="input">The input source entity's input.</param>
	public void ReportUncarried(EntityInput input)
	{
		if (_reportedMissingAction)
		{
			return;
		}

		foreach (string actionName in _actionNames)
		{
			if (!Carries(input, actionName))
			{
				ReportMissingActionOnce(actionName, "the entity's input does not carry");
				return;
			}
		}
	}

	/// <summary>
	/// Gets whether the project's Input Map has every action, reporting the first one it does not.
	/// </summary>
	/// <returns><see langword="true"/> if the device can be read for every action.</returns>
	public bool DeviceHasActions()
	{
		foreach (StringName actionName in _deviceActionNames)
		{
			if (!InputMap.HasAction(actionName))
			{
				ReportMissingActionOnce(actionName, "the project's Input Map does not have");
				return false;
			}
		}

		return true;
	}

	private static bool Carries(EntityInput input, string action)
	{
		return input.Actions.IndexOf(action) >= 0
			|| input.Actions.Vectors.Any(x =>
				x.NegativeX == action || x.PositiveX == action || x.NegativeY == action || x.PositiveY == action);
	}

	private void ReportMissingActionOnce(string actionName, string where)
	{
		if (_reportedMissingAction)
		{
			return;
		}

		_reportedMissingAction = true;

		GD.PushWarning(
			$"Statescript: {_resolverName} names the input action [{actionName}], which {where}. Resolving to a " +
			"default value.");
	}
}
