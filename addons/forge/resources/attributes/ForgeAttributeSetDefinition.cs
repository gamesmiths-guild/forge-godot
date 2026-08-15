// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Attributes;

/// <summary>
/// Describes an attribute set without writing code. Saving one regenerates a matching C# class under
/// <c>res://forge_generated/attribute_sets</c>, which the project then compiles like any hand-written set.
/// </summary>
/// <remarks>
/// The generated class is <see langword="partial"/>, so per-set logic such as clamping one attribute to another can
/// still be added in a separate file that overrides <c>AttributeOnValueChanged</c>, <c>PreEffectExecute</c> or
/// <c>PostEffectExecute</c>.
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeAttributeSetDefinition : Resource
{
	/// <summary>
	/// Gets or sets the set's name. It becomes the generated class name and the prefix of every attribute key, so it
	/// has to be a valid C# identifier and unique across the project.
	/// </summary>
	/// <remarks>
	/// Renaming a set changes every attribute key it owns, which does not update the effects already referencing them.
	/// <para>
	/// Named <c>AttributeSetName</c> rather than <c>SetName</c> because <see cref="Resource"/> already has a
	/// <c>SetName</c> method, which a property of that name would shadow.
	/// </para>
	/// </remarks>
	[Export]
	public string AttributeSetName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the attributes the set owns.
	/// </summary>
	[Export]
	public Array<ForgeAttributeDefinition> Attributes { get; set; } = [];
}
