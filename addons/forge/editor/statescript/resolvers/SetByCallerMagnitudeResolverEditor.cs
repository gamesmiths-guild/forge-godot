// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Linq;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the SetByCaller magnitude stored on an effect for an identifier tag.
/// </summary>
[Tool]
internal sealed partial class SetByCallerMagnitudeResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private string _identifierTag = string.Empty;
	private TagContainerSelectionControl? _tagControl;
	private NestedResolverPicker? _effectPicker;

	public override string DisplayName => "SetByCaller Magnitude";

	public override string ResolverTypeId => "SetByCallerMagnitude";

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
		_onChanged = onChanged;

		var existingResource = property?.Resolver as SetByCallerMagnitudeResolverResource;
		_identifierTag = existingResource?.IdentifierTag ?? string.Empty;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_tagControl = new TagContainerSelectionControl { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_tagControl);
		_tagControl.SetValue(
			string.IsNullOrEmpty(_identifierTag) ? [] : [_identifierTag]);
		_tagControl.ValueChanged += tags =>
		{
			_identifierTag = tags.FirstOrDefault() ?? string.Empty;
			_onChanged?.Invoke();
		};

		_effectPicker = new NestedResolverPicker();
		_effectPicker.Initialize(
			graph,
			existingResource?.Effect,
			"Effect:",
			[typeof(Effect)],
			isArray: false,
			folded: false,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_effectPicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new SetByCallerMagnitudeResolverResource
		{
			IdentifierTag = _identifierTag,
			Effect = _effectPicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_effectPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_effectPicker is not null && _effectPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}
}
#endif
