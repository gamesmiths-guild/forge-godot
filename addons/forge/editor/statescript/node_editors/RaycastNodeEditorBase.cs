// Copyright © Gamesmiths Guild.

#if TOOLS
namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Shared editor for the two ray nodes, which write the same five outputs.
/// </summary>
internal abstract partial class RaycastNodeEditorBase : StandardNodeEditorBase
{
	// Output variable indexes of the two object-lane results, matching RaycastNodeParameters3D.
	private const int HitEntityOutputIndex = 2;
	private const int HitNodeOutputIndex = 3;

	/// <inheritdoc/>
	protected override string? GetOutputObjectTypeId(int outputIndex)
	{
		// The hit entity and the collider are object-lane, so they bind through their object variable type rather than
		// the default value-lane path. The position, normal and distance beside them are ordinary values.
		return outputIndex switch
		{
			HitEntityOutputIndex => "Entity",
			HitNodeOutputIndex => "GodotNode",
			_ => null,
		};
	}
}
#endif
