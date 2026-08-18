// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// The Is Valid resolver, extended with the one check the engine-agnostic library cannot make: whether a Godot object
/// has already been freed.
/// </summary>
/// <remarks>
/// A freed <see cref="GodotObject"/> is not <see langword="null"/> from C#'s point of view, and it implements no
/// validity interface, so without this a queue-freed projectile or a despawned summon would report itself perfectly
/// usable right up until the next node touched it.
/// </remarks>
/// <param name="source">The object-backed resolver whose result is checked.</param>
internal sealed class GodotIsValidResolver(IObjectResolver source) : IsValidResolver(source)
{
	/// <inheritdoc/>
	protected override bool IsValid(object? value)
	{
		return base.IsValid(value)
			&& (value is not GodotObject godotObject || GodotObject.IsInstanceValid(godotObject));
	}
}
