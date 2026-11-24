// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// How hard a collider is for a shot to go through: set it as the collider's <see cref="MetaName"/> metadata.
/// </summary>
/// <remarks>
/// A shot with power goes through a collider that has one, losing <see cref="Resistance"/> times the thickness it
/// crosses, and stops once it has none left. A shot without power, or a collider without one, uses the shot's Pierce
/// Mask instead: it goes through colliders on those layers, as many as it may.
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeShotSurface : Resource
{
	/// <summary>
	/// The metadata name a collider holds its surface under.
	/// </summary>
	public const string MetaName = "forge_shot_surface";

	/// <summary>
	/// Gets or sets the power a shot loses per unit of thickness it crosses. Zero lets every shot through freely.
	/// </summary>
	[Export(PropertyHint.Range, "0,1000,0.01,or_greater")]
	public float Resistance { get; set; } = 10;

	/// <summary>
	/// Gives the surface a collider holds, if any.
	/// </summary>
	/// <param name="collider">The collider.</param>
	/// <returns>The surface, or <see langword="null"/>.</returns>
	public static ForgeShotSurface? Of(GodotObject? collider)
	{
		return collider?.HasMeta(MetaName) == true ? collider.GetMeta(MetaName).As<ForgeShotSurface>() : null;
	}
}
