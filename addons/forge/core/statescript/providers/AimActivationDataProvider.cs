// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Providers;

/// <summary>
/// Activation-data provider for <see cref="AimActivationData"/>, so aim reaches a graph without any game-specific
/// provider code.
/// </summary>
/// <remarks>
/// The declared members are <c>System.Numerics</c> vectors even though the payload holds Godot ones. The two sides are
/// not symmetric: declared members describe what the editor offers when a graph <em>sends</em> aim to another ability,
/// and graph resolvers all produce <c>System.Numerics</c> values, while <em>reading</em> the payload reflects over the
/// struct itself and converts on the way out.
/// </remarks>
public sealed class AimActivationDataProvider : AbilityActivationDataProvider<AimActivationData>
{
	/// <summary>
	/// The name of the origin member.
	/// </summary>
	public const string OriginMember = nameof(AimActivationData.Origin);

	/// <summary>
	/// The name of the direction member.
	/// </summary>
	public const string DirectionMember = nameof(AimActivationData.Direction);

	/// <summary>
	/// The name of the target point member.
	/// </summary>
	public const string TargetPointMember = nameof(AimActivationData.TargetPoint);

	private static readonly AbilityActivationDataMember[] _members =
	[
		new AbilityActivationDataMember(OriginMember, typeof(NumericsVector3)),
		new AbilityActivationDataMember(DirectionMember, typeof(NumericsVector3)),
		new AbilityActivationDataMember(TargetPointMember, typeof(NumericsVector3)),
	];

	/// <inheritdoc/>
	public override IReadOnlyList<AbilityActivationDataMember> Members => _members;

	/// <inheritdoc/>
	public override AimActivationData CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
	{
		return new AimActivationData(
			ToGodot(inputs.Get<NumericsVector3>(OriginMember)),
			ToGodot(inputs.Get<NumericsVector3>(DirectionMember)),
			ToGodot(inputs.Get<NumericsVector3>(TargetPointMember)));
	}

	private static Vector3 ToGodot(NumericsVector3 value)
	{
		return new Vector3(value.X, value.Y, value.Z);
	}
}
