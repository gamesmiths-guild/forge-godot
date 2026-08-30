// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using GodotNode = Godot.Node;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the scene node an entity lives on.
/// </summary>
[Tool]
internal sealed partial class NodeFromEntityResolverEditor : EntityScopedResolverEditorBase
{
	private LineEdit? _nodePathField;

	public override string DisplayName => "Node From Entity";

	public override string ResolverTypeId => "NodeFromEntity";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(GodotNode);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as NodeFromEntityResolverResource;

		InitializeEntityScope(graph, onChanged, resource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow());

		_nodePathField = new LineEdit
		{
			PlaceholderText = "%Muzzle",
			Text = resource?.NodePath ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "Optional. A child node to return instead of the entity's own, such as a %Muzzle marker.",
		};

		_nodePathField.TextChanged += _ => NotifyChanged();

		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(
			"Node:",
			_nodePathField,
			ResolverEditorLayoutUtilities.SettingLabelWidth));
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new NodeFromEntityResolverResource
		{
			EntityResolver = BuildEntityResolverResource(),
			NodePath = _nodePathField is not null && IsInstanceValid(_nodePathField)
				? _nodePathField.Text
				: string.Empty,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _nodePathField is not null && IsInstanceValid(_nodePathField) && _nodePathField.Text.Length > 0
			? _nodePathField.Text
			: "Node From Entity";

		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_nodePathField = null;
	}
}
#endif
