// Copyright © Gamesmiths Guild.

using System;
using System.Reflection;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using GodotPlane = Godot.Plane;
using GodotQuaternion = Godot.Quaternion;
using GodotVector2 = Godot.Vector2;
using GodotVector3 = Godot.Vector3;
using GodotVector4 = Godot.Vector4;
using SysPlane = System.Numerics.Plane;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Activation-data resolver for fields typed as Godot math structs (<see cref="GodotVector2"/>,
/// <see cref="GodotVector3"/>, <see cref="GodotVector4"/>, <see cref="GodotQuaternion"/>, <see cref="GodotPlane"/>).
/// </summary>
/// <remarks>
/// The core <see cref="AbilityActivationDataResolver"/> only boxes types that <see cref="Variant128"/> supports
/// (System.Numerics math types and primitives). This Godot-layer resolver reads the Godot-typed field and converts it
/// to the System.Numerics equivalent before boxing, mirroring how the rest of the Godot integration converts Godot math
/// types at graph boundaries.
/// </remarks>
internal sealed class GodotActivationDataResolver : IPropertyResolver
{
	private readonly Type _activationContextType;
	private readonly PropertyInfo _dataProperty;
	private readonly Func<object, object?> _readMember;
	private readonly Func<object, Variant128> _convert;

	/// <inheritdoc/>
	public Type ValueType { get; }

	private GodotActivationDataResolver(
		Type activationDataType,
		Func<object, object?> readMember,
		Func<object, Variant128> convert,
		Type valueType)
	{
		_activationContextType = typeof(AbilityBehaviorContext<>).MakeGenericType(activationDataType);
		_dataProperty = _activationContextType.GetProperty(nameof(AbilityBehaviorContext<int>.Data))!;
		_readMember = readMember;
		_convert = convert;
		ValueType = valueType;
	}

	/// <summary>
	/// Tries to create a resolver for an activation-data member when its type is a supported Godot math struct.
	/// </summary>
	/// <param name="activationDataType">The activation-data type carried by
	/// <see cref="AbilityBehaviorContext{TData}"/>.
	/// </param>
	/// <param name="memberName">The public field or property to read.</param>
	/// <param name="resolver">The created resolver when the member type is a supported Godot math struct.</param>
	/// <returns><see langword="true"/> when a Godot math member was matched and a resolver created.</returns>
	public static bool TryCreate(Type activationDataType, string memberName, out IPropertyResolver resolver)
	{
		resolver = null!;

		Func<object, object?>? readMember = BuildMemberReader(activationDataType, memberName, out Type? memberType);
		if (readMember is null || memberType is null)
		{
			return false;
		}

		if (!TryGetConverter(memberType, out Func<object, Variant128>? convert, out Type? valueType))
		{
			return false;
		}

		resolver = new GodotActivationDataResolver(activationDataType, readMember, convert!, valueType!);
		return true;
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		object? activationContext = graphContext.ActivationContext;
		if (activationContext is null || !_activationContextType.IsInstanceOfType(activationContext))
		{
			return default;
		}

		object? data = _dataProperty.GetValue(activationContext);
		if (data is null)
		{
			return default;
		}

		object? member = _readMember(data);
		return member is null ? default : _convert(member);
	}

	private static Func<object, object?>? BuildMemberReader(
		Type activationDataType,
		string memberName,
		out Type? memberType)
	{
		PropertyInfo? property = activationDataType.GetProperty(
			memberName,
			BindingFlags.Instance | BindingFlags.Public);
		if (property is { CanRead: true })
		{
			memberType = property.PropertyType;
			return data => property.GetValue(data);
		}

		FieldInfo? field = activationDataType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
		if (field is not null)
		{
			memberType = field.FieldType;
			return data => field.GetValue(data);
		}

		memberType = null;
		return null;
	}

	private static bool TryGetConverter(
		Type memberType,
		out Func<object, Variant128>? convert,
		out Type? valueType)
	{
		if (memberType == typeof(GodotVector2))
		{
			convert = value => new Variant128(ToSys((GodotVector2)value));
			valueType = typeof(SysVector2);
			return true;
		}

		if (memberType == typeof(GodotVector3))
		{
			convert = value => new Variant128(ToSys((GodotVector3)value));
			valueType = typeof(SysVector3);
			return true;
		}

		if (memberType == typeof(GodotVector4))
		{
			convert = value => new Variant128(ToSys((GodotVector4)value));
			valueType = typeof(SysVector4);
			return true;
		}

		if (memberType == typeof(GodotQuaternion))
		{
			convert = value => new Variant128(ToSys((GodotQuaternion)value));
			valueType = typeof(SysQuaternion);
			return true;
		}

		if (memberType == typeof(GodotPlane))
		{
			convert = value => new Variant128(ToSys((GodotPlane)value));
			valueType = typeof(SysPlane);
			return true;
		}

		convert = null;
		valueType = null;
		return false;
	}

	private static SysVector2 ToSys(GodotVector2 value)
	{
		return new SysVector2(value.X, value.Y);
	}

	private static SysVector3 ToSys(GodotVector3 value)
	{
		return new SysVector3(value.X, value.Y, value.Z);
	}

	private static SysVector4 ToSys(GodotVector4 value)
	{
		return new SysVector4(value.X, value.Y, value.Z, value.W);
	}

	private static SysQuaternion ToSys(GodotQuaternion value)
	{
		return new SysQuaternion(value.X, value.Y, value.Z, value.W);
	}

	private static SysPlane ToSys(GodotPlane value)
	{
		return new SysPlane(value.Normal.X, value.Normal.Y, value.Normal.Z, value.D);
	}
}
