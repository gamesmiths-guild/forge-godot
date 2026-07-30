// Copyright © Gamesmiths Guild.

using System;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Builds the stable identifiers the provider registries store in resources.
/// </summary>
internal static class ProviderIdentifiers
{
	/// <summary>
	/// Gets the identifier stored in resources for the given provider type.
	/// </summary>
	/// <param name="type">The provider type.</param>
	/// <returns>The provider identifier (its full name, or simple name when unavailable).</returns>
	public static string For(Type type)
	{
		return type.FullName ?? type.Name;
	}
}
