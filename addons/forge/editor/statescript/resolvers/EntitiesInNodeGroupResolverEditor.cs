// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the entities a project put in a Godot group.
/// </summary>
/// <remarks>
/// Free text rather than a picker, for the same reason the node path constant is: the graph is a resource with no
/// scene of its own, so there is no tree to read the project's groups off at authoring time.
/// </remarks>
[Tool]
internal sealed partial class EntitiesInNodeGroupResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private LineEdit? _groupField;

	public override string DisplayName => "Entities In Node Group";

	public override string ResolverTypeId => "EntitiesInNodeGroup";

	public override bool SupportsScalarValues => false;

	public override bool SupportsArrayValues => true;

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

		_groupField = new LineEdit
		{
			PlaceholderText = "guards",
			Text = (property?.Resolver as EntitiesInNodeGroupResolverResource)?.GroupName ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "The name of a Godot group, as typed in the Node dock's Groups tab. Members carrying no "
				+ "entity are skipped.",
		};

		_groupField.TextChanged += _ => _onChanged?.Invoke();
		AddChild(_groupField);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EntitiesInNodeGroupResolverResource
		{
			GroupName = _groupField is not null && IsInstanceValid(_groupField) ? _groupField.Text : string.Empty,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _groupField is not null && IsInstanceValid(_groupField) && _groupField.Text.Length > 0
			? _groupField.Text
			: "(None)";

		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_groupField = null;
	}
}
#endif
