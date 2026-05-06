// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeEulerOrder = Gamesmiths.Forge.Statescript.Properties.EulerOrder;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class QuaternionFromEulerAnglesResolverResource : UnaryNestedResolverResourceBase
{
	[Export]
	public ForgeEulerOrder Order { get; set; } = ForgeEulerOrder.XYZ;

	public override string ResolverTypeId => "QuaternionFromEulerAngles";

	protected override string PropertyNamePrefix => "__quateuler";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver operandResolver,
		Graph graph)
	{
		return new QuaternionFromEulerAnglesResolver(
			operandResolver,
			new VariantResolver(new Variant128((int)Order), typeof(int)));
	}
}
