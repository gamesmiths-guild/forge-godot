// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Base for resolver editors that read something off an entity, providing the "which entity" operand they all share.
/// </summary>
/// <remarks>
/// The operand is an <see cref="EntityOperandPicker"/>, which is a nested resolver picker over the entity lane. Every
/// registered resolver that produces an entity is therefore selectable here, and a new one becomes available without
/// this base or any of its subclasses changing.
/// </remarks>
internal abstract partial class EntityScopedResolverEditorBase : NodeEditorProperty
{
	private Action? _onChanged;
	private StatescriptGraph? _graph;
	private EntityOperandPicker? _entityPicker;

	/// <summary>
	/// Gets the stored operand passed to <see cref="InitializeEntityScope"/>.
	/// </summary>
	protected StatescriptResolverResource? StoredEntityResolver { get; private set; }

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_entityPicker?.ClearCallbacks();
		_entityPicker = null;
	}

	/// <inheritdoc/>
	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_entityPicker is not null && _entityPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	/// <summary>
	/// Stores what the entity operand needs before it is built.
	/// </summary>
	/// <param name="graph">The graph being edited.</param>
	/// <param name="onChanged">The editor's change callback.</param>
	/// <param name="entityResolver">The stored operand, when there is one.</param>
	protected void InitializeEntityScope(
		StatescriptGraph graph,
		Action onChanged,
		StatescriptResolverResource? entityResolver)
	{
		_graph = graph;
		_onChanged = onChanged;
		StoredEntityResolver = entityResolver;
	}

	/// <summary>
	/// Builds the entity operand row.
	/// </summary>
	/// <param name="label">The row label. Defaults to the operand's usual name.</param>
	/// <returns>The row to add.</returns>
	protected Control CreateEntitySelectorRow(string label = "Entity:")
	{
		_entityPicker = new EntityOperandPicker();
		_entityPicker.Initialize(
			_graph!,
			StoredEntityResolver,
			label,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope,
			allowNone: false,
			folded: true);

		return _entityPicker;
	}

	/// <summary>
	/// Builds the authored entity operand.
	/// </summary>
	/// <returns>The resolver resource, or <see langword="null"/> when nothing was authored.</returns>
	protected StatescriptResolverResource? BuildEntityResolverResource()
	{
		return _entityPicker?.BuildResource();
	}

	/// <summary>
	/// Runs the editor's change callback.
	/// </summary>
	protected void NotifyChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
