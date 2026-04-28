// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class SignedAngleResolverResource : StatescriptResolverResource
{
	[Export]
	public StatescriptResolverResource? From { get; set; }

	[Export]
	public StatescriptResolverResource? To { get; set; }

	[Export]
	public StatescriptResolverResource? Axis { get; set; }

	public override string ResolverTypeId => "SignedAngle";

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		IPropertyResolver resolver = BuildResolver(graph);
		var propertyName = new Forge.Core.StringKey($"__signedangle_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, resolver);
		runtimeNode.BindInput(index, propertyName);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IPropertyResolver fromResolver =
			From?.BuildResolver(graph) ?? new VariantResolver(default, typeof(System.Numerics.Vector2));
		IPropertyResolver toResolver =
			To?.BuildResolver(graph) ?? new VariantResolver(default, typeof(System.Numerics.Vector2));

		if (Axis is not null)
		{
			IPropertyResolver axisResolver = Axis.BuildResolver(graph);
			return new SignedAngleResolver(fromResolver, toResolver, axisResolver);
		}

		return new SignedAngleResolver(fromResolver, toResolver);
	}
}
