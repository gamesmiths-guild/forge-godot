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
/// Resolver editor that reads how far an input action is pressed.
/// </summary>
[Tool]
internal sealed partial class InputActionStrengthResolverEditor : InputResolverEditorBase
{
	private LineEdit? _actionField;

	public override string DisplayName => "Input Action Strength";

	public override string ResolverTypeId => "InputActionStrength";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new InputActionStrengthResolverResource
		{
			ActionName = ReadActionName(_actionField),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		string actionName = ReadActionName(_actionField);
		summary = actionName.Length > 0 ? actionName : "(None)";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_actionField = null;
	}

	protected override void BuildRows(VBoxContainer root, StatescriptResolverResource? existingResolver)
	{
		var resource = existingResolver as InputActionStrengthResolverResource;

		_actionField = AddActionRow(root, "Action:", resource?.ActionName ?? string.Empty);
	}
}
#endif
