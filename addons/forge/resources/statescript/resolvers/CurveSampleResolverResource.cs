// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that samples a Godot <see cref="Curve"/> at a resolved position.
/// </summary>
[Tool]
[GlobalClass]
public partial class CurveSampleResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CurveSample";

	/// <summary>
	/// Gets or sets the curve to sample.
	/// </summary>
	[Export]
	public Curve? Curve { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the sample position.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Time { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__curvesample_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (Curve is null)
		{
			GD.PushError("Statescript: Curve Sample resolver requires a Curve resource.");
			return new VariantResolver(default, typeof(float));
		}

		IPropertyResolver timeResolver = Time?.BuildResolver(graph) ?? new VariantResolver(default, typeof(float));

		return new CurveSampleResolver(new ForgeCurve(Curve), timeResolver);
	}
}
