// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Object variable type for <see cref="ForgeProjectileData"/> references, so a graph can pick which projectile to
/// launch at runtime.
/// </summary>
internal sealed class ProjectileObjectVariableType : StatescriptObjectVariableType<ForgeProjectileData>
{
	public override string TypeId => "Projectile";

	public override string DisplayName => "Projectile";
}
