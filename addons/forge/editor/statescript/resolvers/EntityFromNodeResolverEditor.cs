// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using GodotNode = Godot.Node;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the entity a scene node belongs to.
/// </summary>
[Tool]
internal sealed partial class EntityFromNodeResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _nodeExpectedTypes = [typeof(GodotNode)];

	private Action? _onChanged;
	private NestedResolverPicker? _nodePicker;

	public override string DisplayName => "Entity From Node";

	public override string ResolverTypeId => "EntityFromNode";

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
		var resource = property?.Resolver as EntityFromNodeResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// Not seeded: the node this reads is usually one the graph made or was handed - a spawned instance, a ray's
		// collider - which lives in a variable, and that is what the picker lands on by itself.
		_nodePicker = new NestedResolverPicker();
		_nodePicker.Initialize(
			graph,
			resource?.Node,
			"Node:",
			_nodeExpectedTypes,
			isArray: false,
			resource?.NodeFolded ?? false,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);

		root.AddChild(_nodePicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EntityFromNodeResolverResource
		{
			Node = _nodePicker?.BuildResource(),
			NodeFolded = _nodePicker?.Folded ?? false,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Entity From Node";
		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_nodePicker is not null && _nodePicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_nodePicker?.ClearCallbacks();
		_nodePicker = null;
	}
}
#endif
