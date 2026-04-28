// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeEulerOrder = Gamesmiths.Forge.Statescript.Properties.EulerOrder;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class EulerAnglesFromQuaternionResolverResource : UnaryNestedResolverResourceBase
{
	[Export]
	public ForgeEulerOrder Order { get; set; } = ForgeEulerOrder.XYZ;

	public override string ResolverTypeId => "EulerAnglesFromQuaternion";

	protected override string PropertyNamePrefix => "__eulerfromquat";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new EulerAnglesFromQuaternionResolver(
			operandResolver,
			new VariantResolver(new Variant128((int)Order), typeof(int)));
	}
}
