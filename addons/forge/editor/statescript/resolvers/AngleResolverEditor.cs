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
internal sealed partial class AngleResolverEditor
	: VectorOrQuaternionBinaryFloatResolverEditorBase<AngleResolverResource>
{
	public override string DisplayName => "Angle";

	public override string ResolverTypeId => "Angle";

	protected override string LeftTitle => "From:";

	protected override string RightTitle => "To:";

	protected override Type[] GetFactoryExpectedTypes(Type expectedType)
	{
		if (expectedType == typeof(ForgeVariant128))
		{
			return [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4), typeof(System.Numerics.Quaternion)];
		}
		else if (expectedType == typeof(float))
		{
			return [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4), typeof(System.Numerics.Quaternion)];
		}
		else
		{
			return [expectedType];
		}
	}
}
#endif
