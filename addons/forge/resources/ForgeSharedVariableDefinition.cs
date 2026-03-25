// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// Resource representing a single shared variable definition for an entity, including name, type, and initial value.
/// </summary>
[Tool]
[GlobalClass]
public partial class ForgeSharedVariableDefinition : Resource
{
	/// <summary>
	/// Gets or sets the name of this shared variable.
	/// </summary>
	[Export]
	public string VariableName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the type of this shared variable.
	/// </summary>
	[Export]
	public StatescriptVariableType VariableType { get; set; } = StatescriptVariableType.Int;

	/// <summary>
	/// Gets or sets a value indicating whether this is an array variable.
	/// </summary>
	[Export]
	public bool IsArray { get; set; }

	/// <summary>
	/// Gets or sets the initial value of this shared variable, stored as a Godot variant.
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
