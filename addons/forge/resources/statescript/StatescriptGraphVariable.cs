// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Resource representing a single graph variable definition, including name, type, initial value, and whether it is an
/// array variable.
/// </summary>
[Tool]
public partial class StatescriptGraphVariable : Resource
{
	/// <summary>
	/// Gets or sets the name of this variable.
	/// </summary>
	[Export]
	public string VariableName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the type of this variable.
	/// </summary>
	[Export]
	public StatescriptVariableType VariableType { get; set; } = StatescriptVariableType.Int;

	/// <summary>
	/// Gets or sets a value indicating whether this is an array variable.
	/// </summary>
	[Export]
	public bool IsArray { get; set; }

	/// <summary>
	/// Gets or sets the initial value of this variable, stored as a Godot variant.
	/// For non-array variables, this is a single value. Ignored when <see cref="IsArray"/> is true.
	/// </summary>
	[Export]
	public Variant InitialValue { get; set; }

	/// <summary>
	/// Gets or sets the initial values for array variables.
	/// Each element is stored as a Godot variant. Only used when <see cref="IsArray"/> is true.
	/// </summary>
	[Export]
	public Array<Variant> InitialArrayValues { get; set; } = [];
}
