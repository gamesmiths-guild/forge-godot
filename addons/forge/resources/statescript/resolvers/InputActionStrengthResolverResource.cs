// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads how far an input action is pressed.
/// </summary>
[Tool]
[GlobalClass]
public partial class InputActionStrengthResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "InputActionStrength";

	/// <summary>
	/// Gets or sets the input action to read.
	/// </summary>
	[Export]
	public string ActionName { get; set; } = string.Empty;

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "inputactionstrength";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new InputActionStrengthResolver(ActionName);
	}
}
