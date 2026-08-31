// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads how long an animation runs for, in seconds.
/// </summary>
[Tool]
internal sealed partial class AnimationLengthResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 88.0f;

	private LineEdit? _playerPathField;
	private LineEdit? _animationField;

	public override string DisplayName => "Animation Length";

	public override string ResolverTypeId => "AnimationLength";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var existingResource = property?.Resolver as AnimationLengthResolverResource;

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow());

		// Free text rather than pickers, matching the presentation nodes: the graph cannot enumerate the players or
		// the clips of whichever scene an entity happens to come from.
		_playerPathField = AddTextRow(
			root,
			"Player:",
			existingResource?.PlayerPath,
			"AnimationPlayer",
			"Optional. The animation player to read, as a path from the entity's own node. Empty takes its first.");

		_animationField = AddTextRow(
			root,
			"Animation:",
			existingResource?.Animation,
			"cast_windup",
			"The name of the animation to measure, as the player has it.");
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AnimationLengthResolverResource
		{
			EntityResolver = BuildEntityResolverResource(),
			PlayerPath = ReadText(_playerPathField),
			Animation = ReadText(_animationField),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Animation Length";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_playerPathField = null;
		_animationField = null;
	}

	private static string ReadText(LineEdit? field)
	{
		return field is not null && IsInstanceValid(field) ? field.Text : string.Empty;
	}

	private LineEdit AddTextRow(
		VBoxContainer root,
		string label,
		string? text,
		string placeholder,
		string tooltip)
	{
		var field = new LineEdit
		{
			PlaceholderText = placeholder,
			Text = text ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = tooltip,
		};

		field.TextChanged += _ => NotifyChanged();
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(label, field, LabelWidth));

		return field;
	}
}
#endif
