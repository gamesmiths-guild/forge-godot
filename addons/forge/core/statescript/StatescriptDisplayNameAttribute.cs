// Copyright © Gamesmiths Guild.

using System;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Overrides the name the editor shows for a node type.
/// </summary>
/// <remarks>
/// Display names are normally derived from the type name by stripping the <c>Node</c> suffix and splitting the
/// remaining words, which covers almost every case. Use this attribute when that derivation reads badly and the type
/// cannot simply be renamed.
/// </remarks>
/// <param name="displayName">The name to show, used verbatim.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StatescriptDisplayNameAttribute(string displayName) : Attribute
{
	/// <summary>
	/// Gets the display name to show in the editor.
	/// </summary>
	public string DisplayName { get; } = displayName;
}
