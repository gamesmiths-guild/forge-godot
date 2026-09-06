// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the nodes a project put in a Godot group.
/// </summary>
/// <remarks>
/// Free text rather than a picker, for the same reason the node path constant is: the graph is a resource with no
/// scene of its own, so there is no tree to read the project's groups off at authoring time.
/// </remarks>
[Tool]
internal sealed partial class NodesInNodeGroupResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private LineEdit? _groupField;

	public override string DisplayName => "Nodes In Node Group";

	public override string ResolverTypeId => "NodesInNodeGroup";

	public override bool SupportsScalarValues => false;

	public override bool SupportsArrayValues => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(Node);
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
			PlaceholderText = "spawn_points",
			Text = (property?.Resolver as NodesInNodeGroupResolverResource)?.GroupName ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "The name of a Godot group, as typed in the Node dock's Groups tab.",
		};

		_groupField.TextChanged += _ => _onChanged?.Invoke();
		AddChild(_groupField);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new NodesInNodeGroupResolverResource
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
