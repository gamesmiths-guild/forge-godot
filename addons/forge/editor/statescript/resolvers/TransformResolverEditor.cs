// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysPlane = System.Numerics.Plane;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class TransformResolverEditor
	: AsymmetricBinaryNestedResolverEditorBase<TransformResolverResource>
{
	public override string DisplayName => "Transform";

	public override string ResolverTypeId => "Transform";

	protected override Type[] LeftFactoryExpectedTypes =>
		[typeof(SysVector2), typeof(SysVector3), typeof(SysVector4), typeof(SysPlane)];

	protected override Type[] RightFactoryExpectedTypes => [typeof(SysQuaternion)];

	protected override Type LeftNestedExpectedType => typeof(ForgeVariant128);

	protected override Type RightNestedExpectedType => typeof(SysQuaternion);

	protected override string LeftTitle => "Value:";

	protected override string RightTitle => "Rotation:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector2)
			|| expectedType == typeof(SysVector3)
			|| expectedType == typeof(SysVector4)
			|| expectedType == typeof(SysPlane)
			|| expectedType == typeof(ForgeVariant128);
	}

	protected override Type[] GetLeftFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4), typeof(SysPlane)]
			: [expectedType];
	}
}
#endif
