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
/// Resolver editor that reads the signed strength of a pair of opposed input actions.
/// </summary>
[Tool]
internal sealed partial class InputAxisResolverEditor : InputResolverEditorBase
{
	private LineEdit? _negativeField;
	private LineEdit? _positiveField;

	public override string DisplayName => "Input Axis";

	public override string ResolverTypeId => "InputAxis";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new InputAxisResolverResource
		{
			NegativeAction = ReadActionName(_negativeField),
			PositiveAction = ReadActionName(_positiveField),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Input Axis";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_negativeField = null;
		_positiveField = null;
	}

	protected override void BuildRows(VBoxContainer root, StatescriptResolverResource? existingResolver)
	{
		var resource = existingResolver as InputAxisResolverResource;

		_negativeField = AddActionRow(root, "Negative:", resource?.NegativeAction ?? string.Empty);
		_positiveField = AddActionRow(root, "Positive:", resource?.PositiveAction ?? string.Empty);
	}
}
#endif
