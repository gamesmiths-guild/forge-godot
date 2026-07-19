// Copyright © Gamesmiths Guild.

#if TOOLS
namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Describes a node constructor parameter surfaced as an editor control by <see cref="StandardNodeEditorBase"/>.
/// The parameter value is persisted into the node's <c>CustomData</c> under <see cref="Key"/> (matching the runtime
/// constructor parameter name) and consumed at graph-build time.
/// </summary>
/// <remarks>
/// Enum parameters store the selected enum member <b>name</b> (not its index), so flags enums whose member values do
/// not match their declaration order are round-tripped correctly through the graph builder's name-based parsing.
/// </remarks>
/// <param name="Key">The CustomData key, matching the runtime node constructor parameter name.</param>
/// <param name="Label">The display label for the control.</param>
/// <param name="EnumNames">The enum member names (matching the runtime enum) for an enum parameter, or
/// <see langword="null"/> for a boolean parameter rendered as a checkbox.</param>
/// <param name="DefaultName">The default enum member name, used when no value is stored yet (matches the constructor
/// default).</param>
/// <param name="DefaultBool">The default boolean value, used when no value is stored yet.</param>
internal readonly record struct NodeConfigParam(
	string Key,
	string Label,
	string[]? EnumNames = null,
	string? DefaultName = null,
	bool DefaultBool = false);
#endif
