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
	/// Gets or sets the Variant128 (primitive) type of this shared variable. Ignored when <see cref="ObjectTypeId"/> is
	/// set.
	/// </summary>
	[Export]
	public StatescriptVariableType VariableType { get; set; } = StatescriptVariableType.Int;

	/// <summary>
	/// Gets or sets the object variable type id when this shared variable is object-backed (reference type); empty for
	/// primitive variables.
	/// </summary>
	[Export]
	public string ObjectTypeId { get; set; } = string.Empty;

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
