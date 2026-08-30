// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Linq;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// The entries the interop nodes offer for their type settings.
/// </summary>
/// <remarks>
/// Two lists off one enum. A value being read or written always has a type, so those rows leave None out; an argument
/// row keeps it, because that is how an argument says it is not passed.
/// </remarks>
internal static class InteropValueTypeNames
{
	/// <summary>
	/// The types a value can be, with no entry for the absence of one.
	/// </summary>
	public static readonly string[] Values =
		[.. Enum.GetValues<InteropValueType>().Where(x => x != InteropValueType.None).Select(x => x.ToString())];

	/// <summary>
	/// The types an argument can be, starting with the entry that says there is no argument.
	/// </summary>
	public static readonly string[] Arguments = [.. Enum.GetValues<InteropValueType>().Select(x => x.ToString())];
}
#endif
