// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the entity at an authored scene path.
/// </summary>
/// <remarks>
/// Free text rather than a picker, for the same reason the node path constant is: the graph is a resource with no scene
/// of its own, so there is no tree to browse at authoring time.
/// </remarks>
[Tool]
internal sealed partial class EntityAtPathResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private LineEdit? _pathField;

	public override string DisplayName => "Entity At Path";

	public override string ResolverTypeId => "EntityAtPath";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(IForgeEntity);
	}

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
			PlaceholderText = "%Boss",
			Text = (property?.Resolver as EntityAtPathResolverResource)?.NodePath ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "A path from the current scene's root, to the entity's node or anything under it. Absolute "
				+ "paths (/root/Main/Boss) and scene-unique names (%Boss) work.",
		};

		_pathField.TextChanged += _ => _onChanged?.Invoke();
		AddChild(_pathField);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EntityAtPathResolverResource
		{
			NodePath = _pathField is not null && IsInstanceValid(_pathField) ? _pathField.Text : string.Empty,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _pathField is not null && IsInstanceValid(_pathField) && _pathField.Text.Length > 0
			? _pathField.Text
			: "(None)";

		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_pathField = null;
	}
}
#endif
