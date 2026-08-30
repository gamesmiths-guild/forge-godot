// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the signed strength of a pair of opposed input actions.
/// </summary>
[Tool]
[GlobalClass]
public partial class InputAxisResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "InputAxis";

	/// <summary>
	/// Gets or sets the action driving the value towards −1.
	/// </summary>
	[Export]
	public string NegativeAction { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the action driving the value towards 1.
	/// </summary>
	[Export]
	public string PositiveAction { get; set; } = string.Empty;

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "inputaxis";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new InputAxisResolver(NegativeAction, PositiveAction);
	}
}
