// Copyright © Gamesmiths Guild.

using System;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// A named list of values used to author Statescript integers by name instead of by number — the graph equivalent of a
/// C# enum without explicit values.
/// </summary>
/// <remarks>
/// <para>Members are <b>ordinal</b>: the first member is <c>0</c>, the second <c>1</c>, and so on. That is what makes
/// an enum line up with the nodes that select by index — a <c>SwitchNode</c>'s case ports and a
/// <c>StateMachineNode</c>'s state subgraph ports — so binding an enum to one of those nodes names its ports after the
/// members. Reordering or inserting members therefore renumbers them, exactly as it would for the ports themselves.
/// </para>
/// <para>Enums are an authoring-time concept only. Everything a graph stores and evaluates stays a plain
/// <see langword="int"/>, so an enum can be added to (or removed from) existing graph data at any time.</para>
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeStatescriptEnum : Resource
{
	/// <summary>
	/// Gets or sets the display name for this enum. Falls back to the resource's file name when left empty.
	/// </summary>
	[Export]
	public string EnumName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the member names, in value order: the first entry is value <c>0</c>, the second <c>1</c>, and so
	/// on.
	/// </summary>
	[Export]
	public Array<string> Members { get; set; } = [];

	/// <summary>
	/// Gets the name of this enum for display, falling back to the resource's file name when
	/// <see cref="EnumName"/> is empty.
	/// </summary>
	/// <returns>The display name, or an empty string when the enum is neither named nor saved.</returns>
	public string GetDisplayName()
	{
		if (!string.IsNullOrWhiteSpace(EnumName))
		{
			return EnumName;
		}

		// An enum saved inside another resource has a "<file>::<id>" path whose generated id is no name at all, so it
		// reports as unnamed rather than as something like ":Resource_g1dqc".
		string path = ResourcePath;
		int subResourceIndex = path.IndexOf("::", StringComparison.Ordinal);

		if (subResourceIndex >= 0)
		{
			path = path[..subResourceIndex];
		}

		return string.IsNullOrEmpty(path)
			? string.Empty
			: path.GetFile().GetBaseName();
	}

	/// <summary>
	/// Gets the member name for a value.
	/// </summary>
	/// <param name="value">The value to name.</param>
	/// <returns>The member name, or an empty string when the value is outside the enum.</returns>
	public string GetMemberName(int value)
	{
		return value >= 0 && value < Members.Count ? Members[value] : string.Empty;
	}

	/// <summary>
	/// Gets the value of a member by name.
	/// </summary>
	/// <param name="memberName">The member name to look up.</param>
	/// <returns>The member's value, or <c>-1</c> when the enum has no such member.</returns>
	public int GetMemberValue(string memberName)
	{
		for (int i = 0; i < Members.Count; i++)
		{
			if (string.Equals(Members[i], memberName, StringComparison.Ordinal))
			{
				return i;
			}
		}

		return -1;
	}
}
