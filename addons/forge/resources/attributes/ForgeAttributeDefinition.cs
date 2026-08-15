// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Attributes;

/// <summary>
/// One attribute inside a <see cref="ForgeAttributeSetDefinition"/>, holding everything the generated
/// <c>InitializeAttribute</c> call needs.
/// </summary>
[Tool]
[GlobalClass]
public partial class ForgeAttributeDefinition : Resource
{
	/// <summary>
	/// Gets or sets the attribute's name. It becomes a property on the generated class, so it has to be a valid C#
	/// identifier and unique within its set.
	/// </summary>
	[Export]
	public string AttributeName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the value the attribute starts at.
	/// </summary>
	[Export]
	public int DefaultValue { get; set; }

	/// <summary>
	/// Gets or sets the lowest value the attribute can hold.
	/// </summary>
	[Export]
	public int MinValue { get; set; }

	/// <summary>
	/// Gets or sets the highest value the attribute can hold.
	/// </summary>
	[Export]
	public int MaxValue { get; set; } = int.MaxValue;

	/// <summary>
	/// Gets or sets how many channels the attribute aggregates modifiers through.
	/// </summary>
	[Export(PropertyHint.Range, "1,10,1,or_greater")]
	public int Channels { get; set; } = 1;

	/// <summary>
	/// Gets or sets how many decimal places the stored integer stands for when shown to a player. Presentation only:
	/// the simulation always works with the raw integer.
	/// </summary>
	[Export(PropertyHint.Range, "0,4,1")]
	public int DecimalPlaces { get; set; }
}
