// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Shared editor for the four sweep nodes, which write the same five outputs the ray nodes write and seed the same two
/// rows.
/// </summary>
/// <remarks>
/// Subclasses supply only the shape the Shape row starts on, since that is the one seed that differs between the
/// dimensions.
/// </remarks>
internal abstract partial class ShapecastNodeEditorBase : StandardNodeEditorBase
{
	// Input and output indexes matching ShapecastNodeParameters3D and its 2D twin.
	private const int ShapeInputIndex = 0;
	private const int IgnoreInputIndex = 6;
	private const int HitEntityOutputIndex = 2;
	private const int HitNodeOutputIndex = 3;

	/// <summary>
	/// Builds the shape resolver a fresh Shape row starts on. A new one per call, because it is bound as-is.
	/// </summary>
	/// <returns>The seed resource.</returns>
	protected abstract StatescriptResolverResource BuildDefaultShape();

	/// <inheritdoc/>
	protected override StatescriptResolverResource? GetDefaultInputResolver(int inputIndex)
	{
		return inputIndex switch
		{
			// Seeded so a fresh node sweeps something rather than nothing: an unset shape finds nothing at all, and an
			// unset ignore list stops the sweep on the caster's own collider at zero distance.
			ShapeInputIndex => BuildDefaultShape(),
			IgnoreInputIndex => EntityIgnoreOperand.BuildOwner(),
			_ => null,
		};
	}

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
