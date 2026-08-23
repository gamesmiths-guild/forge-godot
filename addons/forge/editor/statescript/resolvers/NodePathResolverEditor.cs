// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors a constant scene-node reference as a path.
/// </summary>
/// <remarks>
/// Free text rather than a picker for the same reason the spatial getters' node path is: the graph is a resource with
/// no scene of its own, so there is no tree to browse at authoring time.
/// </remarks>
[Tool]
internal sealed partial class NodePathResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private LineEdit? _pathField;

	/// <inheritdoc/>
	public override string DisplayName => "Constant";

	/// <inheritdoc/>
	public override string ResolverTypeId => "NodePath";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(Node);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		_pathField = new LineEdit
		{
			PlaceholderText = "%SpawnPoint",
			Text = (property?.Resolver as NodePathResolverResource)?.NodePath ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "A path from the current scene's root. Absolute paths (/root/Main/Props) and scene-unique "
				+ "names (%SpawnPoint) work.",
		};
		_pathField.TextChanged += _ => _onChanged?.Invoke();
		AddChild(_pathField);
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new NodePathResolverResource
		{
			NodePath = _pathField is not null && IsInstanceValid(_pathField) ? _pathField.Text : string.Empty,
		};
	}

	/// <inheritdoc/>
	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _pathField is not null && IsInstanceValid(_pathField) && _pathField.Text.Length > 0
			? _pathField.Text
			: "(None)";

		return true;
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_pathField = null;
	}
}
#endif
