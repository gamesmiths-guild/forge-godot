// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads whether an input action is down.
/// </summary>
[Tool]
[GlobalClass]
public partial class InputActionPressedResolverResource : ValueResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "InputActionPressed";

	/// <summary>
	/// Gets or sets the input action to read.
	/// </summary>
	/// <remarks>
	/// A name rather than a picker, exactly as animation names are: the action map belongs to the game and is reached
	/// through an engine-generic mechanism, which is what keeps input inside the engine layer's scope.
	/// </remarks>
	[Export]
	public string ActionName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets which question to ask about the button.
	/// </summary>
	[Export]
	public InputActionMode Mode { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "inputactionpressed";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		return new InputActionPressedResolver(ActionName, Mode);
	}
}
