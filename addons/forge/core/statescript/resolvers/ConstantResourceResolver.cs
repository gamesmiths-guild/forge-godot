// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a constant resource authored on the graph, such as a projectile, a blast or a hitscan.
/// </summary>
/// <typeparam name="T">The resource's type.</typeparam>
/// <param name="value">The resource.</param>
internal sealed class ConstantResourceResolver<T>(T? value) : ObjectResolver<T>
	where T : Resource
{
	private readonly T? _value = value;

	public override T? Resolve(GraphContext graphContext)
	{
		return _value;
	}
}
