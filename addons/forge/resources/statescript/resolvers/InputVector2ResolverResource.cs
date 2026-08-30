// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the direction four opposed input actions point in.
/// </summary>
[Tool]
[GlobalClass]
public partial class InputVector2ResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "InputVector2";

	/// <summary>
	/// Gets or sets the action pointing left.
	/// </summary>
	[Export]
	public string LeftAction { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the action pointing right.
	/// </summary>
	[Export]
	public string RightAction { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the action pointing up. Screen axes apply, so this is the one driving Y towards −1.
	/// </summary>
	[Export]
	public string UpAction { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the action pointing down.
	/// </summary>
	[Export]
	public string DownAction { get; set; } = string.Empty;

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "inputvector2";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new InputVector2Resolver(LeftAction, RightAction, UpAction, DownAction);
	}
}
