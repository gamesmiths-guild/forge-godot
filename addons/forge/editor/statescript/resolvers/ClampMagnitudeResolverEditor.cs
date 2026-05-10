// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ClampMagnitudeResolverEditor
	: AsymmetricBinaryNestedResolverEditorBase<ClampMagnitudeResolverResource>
{
	public override string DisplayName => "Clamp Magnitude";

	public override string ResolverTypeId => "ClampMagnitude";

	protected override Type[] LeftFactoryExpectedTypes => [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];

	protected override Type[] RightFactoryExpectedTypes => ResolverEditorCompatibility.FloatOperandExpectedTypes;

	protected override Type LeftNestedExpectedType => typeof(ForgeVariant128);

	protected override Type RightNestedExpectedType => typeof(float);

	protected override string LeftTitle => "Value:";

	protected override string RightTitle => "Max Length:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128) || ResolverEditorCompatibility.IsVectorType(expectedType);
	}

	protected override Type[] GetLeftFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)]
			: [expectedType];
	}
}
#endif
