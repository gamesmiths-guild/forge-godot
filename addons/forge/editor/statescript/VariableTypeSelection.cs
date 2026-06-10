// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Represents a chosen variable type in the editor: either a Variant128 primitive type or a registered object variable
/// type (identified by its type id).
/// </summary>
/// <param name="PrimitiveType">The primitive type, when <see cref="IsObject"/> is <see langword="false"/>.</param>
/// <param name="ObjectTypeId">The object variable type id, when <see cref="IsObject"/> is <see langword="true"/>;
/// otherwise empty.</param>
internal readonly record struct VariableTypeSelection(StatescriptVariableType PrimitiveType, string ObjectTypeId)
{
	/// <summary>
	/// Gets a value indicating whether this selection refers to an object-backed variable type.
	/// </summary>
	public bool IsObject => !string.IsNullOrEmpty(ObjectTypeId);

	/// <summary>
	/// Creates a primitive-type selection.
	/// </summary>
	/// <param name="primitiveType">The primitive type.</param>
	/// <returns>A primitive selection.</returns>
	public static VariableTypeSelection Primitive(StatescriptVariableType primitiveType)
	{
		return new VariableTypeSelection(primitiveType, string.Empty);
	}

	/// <summary>
	/// Creates an object-type selection.
	/// </summary>
	/// <param name="objectTypeId">The object variable type id.</param>
	/// <returns>An object selection.</returns>
	public static VariableTypeSelection Object(string objectTypeId)
	{
		return new VariableTypeSelection(StatescriptVariableType.Int, objectTypeId);
	}
}
#endif
