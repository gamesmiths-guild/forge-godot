// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class QuaternionFromAxisAngleResolverEditor
	: AsymmetricBinaryNestedResolverEditorBase<QuaternionFromAxisAngleResolverResource>
{
	public override string DisplayName => "Quaternion From Axis Angle";

	public override string ResolverTypeId => "QuaternionFromAxisAngle";

	protected override Type[] LeftFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type[] RightFactoryExpectedTypes => ResolverEditorCompatibility.FloatOperandExpectedTypes;

	protected override Type LeftNestedExpectedType => typeof(SysVector3);

	protected override Type RightNestedExpectedType => typeof(float);

	protected override string LeftTitle => "Axis:";

	protected override string RightTitle => "Angle:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysQuaternion) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
