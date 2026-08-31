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
/// <param name="AffectsLayout">When <see langword="true"/>, changing this parameter rebuilds the node so an editor
/// that shows or hides input rows based on it (see <see cref="StandardNodeEditorBase.IsInputVisible"/>) updates
/// immediately. Leave <see langword="false"/> for config that does not change which rows are rendered. This only
/// affects the user's own edit; undo and redo always rebuild so the control re-reads the restored value.</param>
/// <param name="IsText">When <see langword="true"/>, the parameter is a free-text string rendered as a line edit. Used
/// for the names of things the graph cannot enumerate at authoring time - a node path into whichever scene the entity
/// comes from, an animation name, an input action - which is why they are text rather than a picker.</param>
/// <param name="Placeholder">The hint shown in an empty text field, ignored unless <see cref="IsText"/> is set.</param>
/// <param name="SuggestsInputActions">When <see langword="true"/>, the text field is given a dropdown offering the
/// project's input actions. Ignored unless <see cref="IsText"/> is set.</param>
/// <param name="RetypesInput">The index of the input property whose declared type or shape this setting decides, for
/// settings that do more than change which rows are drawn. Such a change invalidates whatever resolver the slot holds -
/// a scalar constant does not belong on a row that now expects a node - so the change is routed through
/// <see cref="CustomNodeEditor.ChangeInputPropertyConfig"/>, which resets the binding and lets the rebuild seed the
/// default for the new type, all as one undoable step. <see cref="AffectsLayout"/> alone rebuilds the node but keeps
/// the stale binding, which then reads back as a row showing one thing and a graph running another. Applies to enum and
/// boolean settings; a text setting never decides an input's type.</param>
internal readonly record struct NodeConfigParam(
	string Key,
	string Label,
	string[]? EnumNames = null,
	string? DefaultName = null,
	bool DefaultBool = false,
	bool AffectsLayout = false,
	bool IsText = false,
	string Placeholder = "",
	bool SuggestsInputActions = false,
	int? RetypesInput = null);
#endif
