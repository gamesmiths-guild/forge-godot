// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Tags;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// One way a tag key can be stored in an asset file.
/// </summary>
/// <param name="Script">
/// The script whose resource carries the property, or an empty identity for a property matched by name alone.
/// </param>
/// <param name="PropertyName">The property that holds the tag key or keys.</param>
/// <param name="IsList">Whether the property holds a list of keys rather than a single one.</param>
/// <param name="OnNodes">
/// Whether the property lives on a scene node rather than on a resource. Used for properties declared by a Forge node
/// type that user scripts derive from, where the script written into the scene is the user's own and cannot be known
/// in advance - so the property name is all there is to match on.
/// </param>
internal readonly record struct TagPropertyDescriptor(
	ScriptIdentity Script,
	string PropertyName,
	bool IsList,
	bool OnNodes);
#endif
