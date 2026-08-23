// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Providers;

/// <summary>
/// Effect context-data provider that carries a single <see cref="float"/> into an effect application.
/// </summary>
/// <remarks>
/// <para>The common case is a scalar the graph computed and the effect needs: a distance falloff multiplier, a charge
/// ratio, a stack scale. A custom execution or magnitude calculator reads it back with
/// <c>TryGetContextData(out float value)</c>.</para>
/// <para>The data type is <see cref="float"/> rather than <see cref="double"/> on purpose: context data is matched by
/// exact type, so a provider of <see cref="double"/> would silently fail to satisfy a calculator asking for a
/// <see cref="float"/>. The declared input is float too, which makes the resolver layer coerce a double-producing
/// resolver rather than reading the wrong half of the value union.</para>
/// </remarks>
public sealed class FloatContextDataProvider : EffectContextDataProvider<float>
{
	/// <summary>
	/// The name of the declared value input.
	/// </summary>
	public const string ValueInput = "Value";

	private static readonly EffectContextDataInput[] _inputs = [new EffectContextDataInput(ValueInput, typeof(float))];

	/// <inheritdoc/>
	public override IReadOnlyList<EffectContextDataInput> Inputs => _inputs;

	/// <inheritdoc/>
	public override float CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
	{
		return inputs.Get<float>(ValueInput);
	}
}
