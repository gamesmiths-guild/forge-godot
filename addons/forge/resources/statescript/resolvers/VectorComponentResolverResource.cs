// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class VectorComponentResolverResource : UnaryNestedResolverResourceBase
{
	[Export]
	public StatescriptVariableType OperandType { get; set; } = StatescriptVariableType.Vector3;

	[Export]
	public VectorComponent Component { get; set; } = VectorComponent.X;

	public override string ResolverTypeId => "VectorComponent";

	protected override string PropertyNamePrefix => "__vectorcomponent";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new VectorComponentResolver(operandResolver, Component);
	}
}
