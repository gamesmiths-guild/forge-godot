// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Providers;

/// <summary>
/// Activation-data provider for <see cref="AimActivationData2D"/>, so aim reaches a graph without any game-specific
/// provider code.
/// </summary>
/// <remarks>
/// The declared members are <c>System.Numerics</c> vectors even though the payload holds Godot ones. The two sides are
/// not symmetric: declared members describe what the editor offers when a graph <em>sends</em> aim to another ability,
/// and graph resolvers all produce <c>System.Numerics</c> values, while <em>reading</em> the payload reflects over the
/// struct itself and converts on the way out.
/// </remarks>
public sealed class AimActivationData2DProvider : AbilityActivationDataProvider<AimActivationData2D>
{
	/// <summary>
	/// The name of the origin member.
	/// </summary>
	public const string OriginMember = nameof(AimActivationData2D.Origin);

	/// <summary>
	/// The name of the direction member.
	/// </summary>
	public const string DirectionMember = nameof(AimActivationData2D.Direction);

	/// <summary>
	/// The name of the target point member.
	/// </summary>
	public const string TargetPointMember = nameof(AimActivationData2D.TargetPoint);

	private static readonly AbilityActivationDataMember[] _members =
	[
		new AbilityActivationDataMember(OriginMember, typeof(NumericsVector2)),
		new AbilityActivationDataMember(DirectionMember, typeof(NumericsVector2)),
		new AbilityActivationDataMember(TargetPointMember, typeof(NumericsVector2)),
	];

	/// <inheritdoc/>
	public override IReadOnlyList<AbilityActivationDataMember> Members => _members;

	/// <inheritdoc/>
	public override AimActivationData2D CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
	{
		return new AimActivationData2D(
			ToGodot(inputs.Get<NumericsVector2>(OriginMember)),
			ToGodot(inputs.Get<NumericsVector2>(DirectionMember)),
			ToGodot(inputs.Get<NumericsVector2>(TargetPointMember)));
	}

	private static Vector2 ToGodot(NumericsVector2 value)
	{
		return new Vector2(value.X, value.Y);
	}
}
