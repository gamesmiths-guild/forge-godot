// Copyright © Gamesmiths Guild.

using System;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Interop;

/// <summary>
/// Reads how many arguments a signal declares, and builds a handler that accepts exactly that many.
/// </summary>
/// <remarks>
/// Godot matches a connection by arity: a callable taking fewer arguments than the signal emits is an error on every
/// emission, not a silent truncation. A listener that reports an edge and discards the payload therefore still has to
/// be shaped like the signal it watches, which is what this builds.
/// </remarks>
internal static class SignalArguments
{
	/// <summary>
	/// The largest argument count a handler can be built for, which is the arity Godot's own callable factory reaches.
	/// </summary>
	public const int MaxArguments = 9;

	/// <summary>
	/// Gets how many arguments a signal declares.
	/// </summary>
	/// <param name="target">The object declaring the signal.</param>
	/// <param name="signalName">The signal to look up.</param>
	/// <returns>The argument count, or -1 when the object declares no such signal.</returns>
	public static int GetArgumentCount(GodotObject target, StringName signalName)
	{
		foreach (GodotDictionary signal in target.GetSignalList())
		{
			if (!signal.TryGetValue("name", out Variant name) || name.AsString() != signalName.ToString())
			{
				continue;
			}

			return signal.TryGetValue("args", out Variant args) && args.VariantType == Variant.Type.Array
				? args.AsGodotArray().Count
				: 0;
		}

		return -1;
	}

	/// <summary>
	/// Builds a callable of the given arity that discards its arguments and runs the handler.
	/// </summary>
	/// <param name="argumentCount">The number of arguments the signal declares.</param>
	/// <param name="handler">What to run when the signal fires.</param>
	/// <param name="callable">The callable to connect, when one could be built.</param>
	/// <returns><see langword="true"/> when a callable of that arity could be built.</returns>
	public static bool TryCreateCallable(int argumentCount, Action handler, out Callable callable)
	{
		callable = argumentCount switch
		{
			0 => Callable.From(handler),
			1 => Callable.From((Variant _) => handler()),
			2 => Callable.From((Variant _, Variant _) => handler()),
			3 => Callable.From((Variant _, Variant _, Variant _) => handler()),
			4 => Callable.From((Variant _, Variant _, Variant _, Variant _) => handler()),
			5 => Callable.From((Variant _, Variant _, Variant _, Variant _, Variant _) => handler()),
			6 => Callable.From((Variant _, Variant _, Variant _, Variant _, Variant _, Variant _) => handler()),
			7 => Callable.From(
				(Variant _, Variant _, Variant _, Variant _, Variant _, Variant _, Variant _) => handler()),
			8 => Callable.From(
				(Variant _, Variant _, Variant _, Variant _, Variant _, Variant _, Variant _, Variant _) => handler()),
			9 => Callable.From(
				(Variant _, Variant _, Variant _, Variant _, Variant _, Variant _, Variant _, Variant _, Variant _) =>
					handler()),
			_ => default,
		};

		return argumentCount is >= 0 and <= MaxArguments;
	}
}
